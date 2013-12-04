using AI.Pitch;
using CUETools.Codecs;
using CUETools.Codecs.FLAKE;
using Microsoft.Speech.Recognition;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;

namespace AI
{
    /// <summary>
    /// Слух
    /// </summary>
    internal class Hearing
    {
        bool _IsSayEva = false;
        bool _IsStart = false;
        Memory memory = new Memory();
        public delegate void dlHeardHerName();
        public delegate void ErrorHeardEventHandler();
        public delegate void HeardEventHandler(object sender, HeardEventArgs e);
        
        /// <summary>
        /// Событие возникает, когда Ева услышала свое имя
        /// </summary>
        public event dlHeardHerName HeardHerName;

        /// <summary>
        /// Событие возникает, когда Ева что-то услышала
        /// </summary>
        public event HeardEventHandler HeardEvent;

        /// <summary>
        /// Событие возникает, когда произходит ошибка при прослушивании
        /// </summary>
        public event ErrorHeardEventHandler ErrorHeard;

        public bool IsSayEva
        {
            get { return _IsSayEva; }
            set { _IsSayEva = value; }
        }

        public bool IsStart
        {
            get { return _IsStart; }
        }

        public Hearing()
        {

        }

        public void Start()
        {
            Thread thread = new Thread(delegate()
            {
                StartRecording();
            });
            thread.Start();

            //StartRecording();
            _IsStart = true;
        }

        private void StartRecording()
        {
            var micManager = new MicManager();
            micManager.HeardEvent += micManager_HeardEvent;
            micManager.ErrorHeard += micManager_ErrorHeard;
        }

        void micManager_ErrorHeard()
        {
            if (ErrorHeard != null)
            {
                ErrorHeard();
            }
        }

        void micManager_HeardEvent(object sender, HeardEventArgs e)
        {
            if (e.Text == "ева")
            {
                memory.WhatSay = e.Text;
                if (HeardHerName != null)
                {
                    IsSayEva = true;
                    HeardHerName();
                }
            }
            else if (e.Text.EndsWith("ева"))
            {
                memory.WhatHeard = e.Text;
                if (HeardEvent != null)
                {
                    switch (e.Text.Replace("ева", "").Trim())
                    {
                        case "привет":
                            IsSayEva = true;
                            HeardEvent(this, new HeardEventArgs("привет"));
                            break;
                        default:
                            HeardEvent(this, new HeardEventArgs("Я не поняла, что Вы сказали."));
                            break;
                    }
                }
            }
            else if (e.Text.StartsWith("ева"))
            {
                memory.WhatHeard = e.Text;
                switch (e.Text.Replace("ева", "").Trim())
                {
                    case "ты тут":
                        IsSayEva = true;
                        if (HeardHerName != null)
                        {
                            IsSayEva = true;
                            HeardHerName();
                        }
                        break;
                    default:
                        HeardEvent(this, new HeardEventArgs("Я не поняла, что Вы сказали."));
                        break;
                }
            }
            else
            {
                if (IsSayEva)
                {
                    memory.WhatHeard = e.Text;
                    if (HeardEvent != null)
                    {
                        IsSayEva = false;
                        HeardEvent(this, new HeardEventArgs(e.Text));
                    }
                }
            }
        } 
    }

    internal class MicManager
    {
        private WaveInEvent waveInEvent;
        private WaveFileWriter waveStream;
        private MemoryStream waveStreamMemory;
        private bool IsPresentVoice = false;
        PitchTracker pitchTracker = new PitchTracker();

        public delegate void ErrorHeardEventHandler();
        public delegate void HeardEventHandler(object sender, HeardEventArgs e);

        /// <summary>
        /// Событие возникает, когда Ева что-то услышала
        /// </summary>
        public event HeardEventHandler HeardEvent;

        /// <summary>
        /// Событие возникает, когда произходит ошибка при прослушивании
        /// </summary>
        public event ErrorHeardEventHandler ErrorHeard;

        public MicManager()
        {
            pitchTracker.SampleRate = 16000.0;
            pitchTracker.PitchDetected += pitchTracker_PitchDetected;

            waveInEvent = new WaveInEvent();
            waveInEvent.DataAvailable += WaveOnDataAvailable;
            waveInEvent.WaveFormat = new NAudio.Wave.WaveFormat(16000, 1); // 16kHz mono
            waveInEvent.StartRecording();
        }

        public float[] ConvertByteToFloat(byte[] array)
        {
            return array.Select(b => (float)b).ToArray();
        }

        void pitchTracker_PitchDetected(PitchTracker sender, PitchTracker.PitchRecord pitchRecord)
        {
            if (pitchRecord.Pitch > 0 && waveStreamMemory != null)
            {
                byte[] tempBytes = Wav2Flac(waveStreamMemory);
                JSon.RecognitionResult recResult = null;

                if (tempBytes != null)
                {
                    //Передаем данные в Google и получаем результат
                    recResult = JSon.Parse(GoogleSpeechRequest(tempBytes));
                }

                waveStream.Close();
                waveStreamMemory.Close();
                waveStreamMemory = null;
                waveStream = null;
                IsPresentVoice = false;

                // Проверяем, что полученные от Google данные - коректны
                if (recResult != null && recResult.hypotheses != null)
                {
                    IsPresentVoice = false;
                    //Log.Write("Ева закончила Вас слушать.", Log.Category.Information);
                    if (recResult.status == "0")
                    {
                        foreach (var item in recResult.hypotheses)
                        {
                            if (item.confidence > 0.5)
                            {
                                if (HeardEvent != null)
                                {
                                    HeardEvent(this, new HeardEventArgs(item.utterance));
                                }
                            }
                            else if (item.utterance == "ева")
                            {
                                if (HeardEvent != null)
                                {
                                    HeardEvent(this, new HeardEventArgs(item.utterance));
                                }
                            }
                        }
                    }
                }
            }
        }

        private void WaveOnDataAvailable(object sender, WaveInEventArgs e)
        {
            VoiceDetection(e);
        }

        private void VoiceDetection(WaveInEventArgs e)
        {
            double AudioThresh = 0.2;
            bool result = false;
            bool Tr = false;
            double Sum2 = 0;
            int Count = e.BytesRecorded / 2;
            for (int index = 0; index < e.BytesRecorded; index += 2)
            {
                double Tmp = (short)((e.Buffer[index + 1] << 8) | e.Buffer[index + 0]);
                Tmp /= 32768.0;
                Sum2 += Tmp * Tmp;
                if (Tmp > AudioThresh)
                    Tr = true;
            }
            Sum2 /= Count;

            // If the Mean-Square is greater than a threshold, set a flag to indicate that noise has happened
            if (Tr || Sum2 > AudioThresh)
            {
                result = true;
            }

            if (result)
            {
                if (waveStream == null)
                {
                    waveStreamMemory = new MemoryStream();
                    waveStream = new WaveFileWriter(waveStreamMemory, waveInEvent.WaveFormat);
                }

                waveStream.Write(e.Buffer, 0, e.BytesRecorded);
                IsPresentVoice = true;
            }
            else
            {
                if (waveStream != null && waveStreamMemory != null)
                {
                    // Был ли голос в записи
                    if (IsPresentVoice)
                    {
                        pitchTracker.ProcessBuffer(ConvertByteToFloat(waveStreamMemory.ToArray()));
                    }
                }
            }
        }

        private byte[] Wav2Flac(MemoryStream waveStreamMemory)
        {
            if (waveStreamMemory.Length >= 3200) // 0
            {
                Log.Write("Ева услышала Вас.", Log.Category.Information);
                byte[] Array = null;

                WaveFileWriter writer = new WaveFileWriter("C:\\rec_temp.wav", waveInEvent.WaveFormat);
                writer.Write(waveStreamMemory.ToArray(), 0, waveStreamMemory.ToArray().Length);
                writer.Close();

                IAudioSource audioSource = new WAVReader("C:\\rec_temp.wav", null);
                AudioPCMConfig audioPCMConfig = new AudioPCMConfig(16, 1, 16000);

                AudioBuffer buff = new AudioBuffer(audioSource, 0x10000);
                //AudioBuffer buffMemory = new AudioBuffer(audioPCMConfig, waveStreamMemory.ToArray(), Convert.ToInt32(waveStreamMemory.Length) / audioPCMConfig.BlockAlign);

                FlakeWriter flakewriter = new FlakeWriter(null, audioSource.PCM);

                FlakeWriter audioDest = flakewriter;
                while (audioSource.Read(buff, -1) != 0)
                {
                    audioDest.Write(buff);
                }

                if (flakewriter.BufferMemory != null)
                {
                    Array = flakewriter.BufferMemory.ToArray();
                }

                audioDest.Close();
                audioSource.Close();

                return Array;
            }

            return null;
        }

        private string GoogleSpeechRequest(byte[] flacBytes)
        {
            if (flacBytes != null)
            {
                try
                {
                    Log.Write("Ева отправляет данные в Google.", Log.Category.Information);
                    WebRequest request = WebRequest.Create("https://www.google.com/speech-api/v1/recognize?xjerr=1&client=chromium&lang=ru-RU");
                    request.Method = "POST";

                    byte[] byteArray = flacBytes;

                    // Set the ContentType property of the WebRequest.
                    request.ContentType = "audio/x-flac; rate=16000";
                    request.ContentLength = byteArray.Length;

                    // Get the request stream.
                    Stream dataStream = request.GetRequestStream();
                    // Write the data to the request stream.
                    dataStream.Write(byteArray, 0, byteArray.Length);

                    dataStream.Close();

                    // Get the response.
                    WebResponse response = request.GetResponse();

                    dataStream = response.GetResponseStream();
                    // Open the stream using a StreamReader for easy access.
                    StreamReader reader = new StreamReader(dataStream);
                    // Read the content.
                    string responseFromServer = reader.ReadToEnd();

                    // Clean up the streams.
                    reader.Close();
                    dataStream.Close();
                    response.Close();

                    Log.Write("Ева получила ответ от Google: " + responseFromServer, Log.Category.Information);
                    return responseFromServer;
                }
                catch (Exception error)
                {
                    if (ErrorHeard != null)
                    {
                        ErrorHeard();
                    }

                    Log.Write(error.Message, Log.Category.Error);
                }
            }

            return null;
        }
    }

    internal class JSon
    {
        [DataContract]
        public class RecognizedItem
        {
            [DataMember]
            public string utterance;

            [DataMember]
            public float confidence;
        }

        [DataContract]
        public class RecognitionResult
        {
            [DataMember]
            public string status;

            [DataMember]
            public string id;

            [DataMember]
            public RecognizedItem[] hypotheses;
        }

        public static RecognitionResult Parse(String toParse)
        {
            if (String.IsNullOrWhiteSpace(toParse))
            {
                return null;
            }

            DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(RecognitionResult));

            MemoryStream stream1 = new MemoryStream(ASCIIEncoding.UTF8.GetBytes(toParse));

            try
            {
                RecognitionResult result = (RecognitionResult)ser.ReadObject(stream1);
                return result;
            }
            catch (SerializationException )
            {
                
            }

            return null;
        }
    }

    public class HeardEventArgs : EventArgs
    {
        public HeardEventArgs(string s) { Text = s; }
        public String Text {get; private set;} // readonly
    }
}
