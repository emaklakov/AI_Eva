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
using System.Threading.Tasks;

namespace AI
{
    /// <summary>
    /// Слух
    /// </summary>
    internal class Hearing
    {
        public delegate void dlHeardHerName();
        
        /// <summary>
        /// Событие возникает, когда Ева услышала свое имя
        /// </summary>
        public event dlHeardHerName HeardHerName;

        public Hearing()
        {

        }

        public void Start()
        {
            //Thread thread = new Thread(delegate()
            //{
            //    StartRecording();
            //});
            //thread.Start();

            StartRecording();
        }

        private void StartRecording()
        {
            var micManager = new MicManager();
            micManager.HeardEvent += micManager_HeardEvent;
        }

        void micManager_HeardEvent(object sender, HeardEventArgs e)
        {
            if (e.Text == "ева")
            {
                if (HeardHerName != null)
                {
                    HeardHerName();
                }
            }
        }

        
    }

    internal class MicManager
    {
        private WaveInEvent waveInEvent;
        private WaveFileWriter waveStream;
        private MemoryStream waveStreamMemory;
        public delegate void dlHeard();
        
        //edit the value of the variable accordingly
        private double AudioThresh = 0.05;

        /// <summary>
        /// Событие возникает, когда Ева что-то услышала
        /// </summary>
        public delegate void HeardEventHandler(object sender, HeardEventArgs e);

        // Declare the event.
        public event HeardEventHandler HeardEvent;

        public MicManager()
        {
            waveInEvent = new WaveInEvent();
            waveInEvent.DataAvailable += WaveOnDataAvailable;
            waveInEvent.WaveFormat = new WaveFormat(16000, 1); // 16kHz mono
            waveInEvent.StartRecording();
        }

        private void WaveOnDataAvailable(object sender, WaveInEventArgs e)
        {
            bool result = ProcessData(e);
            if (result)
            {
                if (waveStream == null)
                {
                    waveStreamMemory = new MemoryStream();
                    waveStream = new WaveFileWriter(waveStreamMemory, waveInEvent.WaveFormat);
                    Log.Write("Ева начала Вас слушать.");
                }

                waveStream.Write(e.Buffer, 0, e.BytesRecorded);
            }
            else
            {
                if (waveStream != null)
                {
                    
                    //Передаем данные в Google и получаем результат
                    JSon.RecognitionResult recResult = JSon.Parse(GoogleSpeechRequest(Wav2Flac(waveStreamMemory)));

                    waveStream.Close();
                    waveStreamMemory.Close();
                    waveStreamMemory = null;
                    waveStream = null;

                    if (recResult != null && recResult.hypotheses != null)
                    {
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
                            }
                        }
                    }

                    Log.Write("Ева закончила Вас слушать.");
                }
            }
        }

        //calculate the sound level based on the AudioThresh
        private bool ProcessData(WaveInEventArgs e)
        {
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
            else
            {
                result = false;
            }
            return result;
        }

        private byte[] Wav2Flac(MemoryStream waveStreamMemory)
        {
            if (waveStreamMemory.Length >= 16000)
            {
                WaveFileWriter writer = new WaveFileWriter("C:\\rec_temp.wav", waveInEvent.WaveFormat);
                writer.Write(waveStreamMemory.ToArray(), 0, waveStreamMemory.ToArray().Length);
                writer.Close();

                IAudioSource audioSource = new WAVReader("C:\\rec_temp.wav", null);
                //AudioPCMConfig audioPCMConfig = new AudioPCMConfig(16, 1, 16000);
                //AudioBuffer buff = new AudioBuffer(audioPCMConfig, waveStreamMemory.ToArray(), 0);

                //FlakeWriter flakewriter = new FlakeWriter("C:\\rec_temp.flac", waveStreamMemory, audioPCMConfig);
                AudioBuffer buff = new AudioBuffer(audioSource, 0x10000);

                FlakeWriter flakewriter = new FlakeWriter("C:\\rec_temp.flac", audioSource.PCM);

                FlakeWriter audioDest = flakewriter;
                while (audioSource.Read(buff, -1) != 0)
                {
                    audioDest.Write(buff);
                }
                audioDest.Close();
                audioDest.Close();
                audioSource.Close();

                byte[] Array = File.ReadAllBytes("C:\\rec_temp.flac");

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
                    Log.Write("Ева отправляет данные в Google.");
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

                    Log.Write("Ева получила ответ от Google: " + responseFromServer);
                    return responseFromServer;
                }
                catch (Exception error)
                {
                    Log.Write(error.Message);
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

            RecognitionResult result = (RecognitionResult)ser.ReadObject(stream1);
            return result;
        }
    }

    public class HeardEventArgs : EventArgs
    {
        public HeardEventArgs(string s) { Text = s; }
        public String Text {get; private set;} // readonly
    }

    internal class Log
    {
        public static void Write(string Text)
        {
            Console.WriteLine(Text);
        }
    }
}
