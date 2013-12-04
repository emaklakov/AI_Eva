using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AI
{
    public class Eva
    {
        Hearing hearing; // Слух
        Voice voice; // Голос
        Log log = new Log(); // Проводим инициализацию записи лога
        Memory memory; // Память

        public Eva()
        {
            memory = new Memory();
            hearing = new Hearing();
            voice = new Voice();
        }

        public void Listen()
        {
            hearing.HeardHerName += hearing_HeardHerName;
            hearing.HeardEvent += hearing_HeardEvent;
            hearing.ErrorHeard += hearing_ErrorHeard;
            hearing.Start();
        }

        void hearing_ErrorHeard()
        {
            voice.Say("Я Вас услышала, но произошла ошибка и я не могу Вам ответить.");
        }

        void hearing_HeardHerName()
        {
            voice.Start();
        }

        void hearing_HeardEvent(object sender, HeardEventArgs e)
        {
            if (e.Text == "привет")
            {
                voice.Say("Привет!");
                voice.Start();
            }
            else if (e.Text == "что ты сказала")
            {
                voice.Say("Я сказала.");
                voice.Say(memory.WhatSay);
            }
            else
            {
                // TODO: 1. Получить список возможных действий. (Команда | Действие | Фраза о подтверждении выполнения работы)
                // TODO: 2. Найти команду. Если команда не найдена, то сказать, что она не знает о чем вы говорите.
                // TODO: 3. Если команда найдена, то сказать, фразу о подтверждении работы и выполнить действие.
                //voice.Say(e.Text);
            }
        }
    }

    internal class Log
    {
        static bool EnableInformation = false;
        static bool EnableAttention = false;
        static bool EnableWarning = false;
        static bool EnableError = false;

        public Log()
        {
            //TODO: AI.Log: Получать параметры категорий из файла конфигураций
            EnableInformation = true;
            EnableAttention = true;
            EnableWarning = true;
            EnableError = true;
        }

        public static void Write(string Text, Category Category)
        {
            switch (Category)
            {
                case Category.Information:
                    if (!EnableInformation)
                    {
                        return;
                    }
                    break;
                case Category.Attention:
                    if (!EnableAttention)
                    {
                        return;
                    }
                    break;
                case Category.Warning:
                    if (!EnableWarning)
                    {
                        return;
                    }
                    break;
                case Category.Error:
                    if (!EnableError)
                    {
                        return;
                    }
                    break;
            }

            Console.WriteLine(String.Format("[{0}] {1}: {2}", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"), Category, Text));
        }

        internal enum Category
        {
            Information,
            Attention,
            Warning,
            Error
        }
    }
}
