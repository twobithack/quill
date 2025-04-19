using System;
using System.Threading;

using OpenTK.Audio.OpenAL;

namespace Quill.Client;

public class Audio
{
  #region Constants
  private const int BUFFER_COUNT = 4;
  #endregion

  #region Fields
  private readonly Func<byte[]> _requestNextBuffer;
  private readonly Thread _bufferingThread;
  private readonly ALDevice _device;
  private readonly ALFormat _format;

  private readonly int _sampleRate;
  private readonly int[] _buffers;
  private readonly int _source;
  private bool _playing;
  #endregion

  public Audio(int sampleRate, Func<byte[]> requestNextBuffer)
  {
    _device = ALC.OpenDevice(null);
    var context = ALC.CreateContext(_device, (int[])null);
    ALC.MakeContextCurrent(context);
    
    _requestNextBuffer = requestNextBuffer;
    _bufferingThread = new Thread(BufferAudio) { IsBackground = true };
    
    _buffers = AL.GenBuffers(BUFFER_COUNT);
    _source = AL.GenSource();

    _sampleRate = sampleRate;
    _format = ALFormat.Mono16;
    
    var silence = new byte[(_sampleRate / 100) * sizeof(short)];
    for (int buffer = 0; buffer < BUFFER_COUNT; buffer++)
      AL.BufferData(_buffers[buffer], _format, silence, _sampleRate);
    AL.SourceQueueBuffers(_source, BUFFER_COUNT, _buffers);
  }

  #region Methods
  public void Play()
  {
    _playing = true;
    _bufferingThread.Start();
    AL.SourcePlay(_source);
  }

  public void Stop()
  {
    _playing = false;
    AL.SourceStop(_source);
    AL.DeleteSource(_source);
    AL.DeleteBuffers(_buffers.Length, _buffers);
    ALC.CloseDevice(_device);
  }

  private void BufferAudio()
  {
    var spinner = new SpinWait();
    while (_playing)
    {
      AL.GetSource(_source, ALGetSourcei.BuffersProcessed, out int processed);
      if (processed == 0)
      {
        spinner.SpinOnce();
        continue;
      }

      while (processed > 0)
      {
        var buffer = AL.SourceUnqueueBuffer(_source);
        var data = _requestNextBuffer();
        AL.BufferData(buffer, _format, data, _sampleRate);
        AL.SourceQueueBuffer(_source, buffer);
        processed--;
      }

      AL.GetSource(_source, ALGetSourcei.SourceState, out int state);
      if ((ALSourceState)state != ALSourceState.Playing)
        AL.SourcePlay(_source);
    }
  }
  #endregion
}
