using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Speech.Recognition;
using Salaam;

namespace TestSolutionMultipleSpeaker
{
    class Program
    {
        static Data data;
        static string directory = "C:\\Users\\Administrator\\Documents\\Visual Studio 2008\\Projects\\cmuspeechrecognition_cmuspeechmain\\test_data";
        static int MSpeakers = 3;
        static int MWordTypes = 2;
        static int MSamples = 5;
        static void Main(string[] args)
        {
            // speakers to be tested
            List<int> speakersToBeTested = new List<int>();
            speakersToBeTested.Add(0);
            speakersToBeTested.Add(1);
            speakersToBeTested.Add(2);

            // words to be tested
            List<int> wordsToBeTested = new List<int>();
            for (int i = 1; i <= 50; i += 5)
                wordsToBeTested.Add(i);
            //wordsToBeTested.Add(25);


            // samples to be tested
            List<int> samplesToBeTested = new List<int>();
            samplesToBeTested.Add(1);
            samplesToBeTested.Add(2);
            samplesToBeTested.Add(3);
            samplesToBeTested.Add(4);
            samplesToBeTested.Add(5);

            // audio directory
            List<string> audioDirectory = new List<string>();
            audioDirectory.Add("C:\\Users\\Administrator\\Documents\\Visual Studio 2008\\Projects\\cmuspeechrecognition_cmuspeechmain\\test_data\\audio\\092910_123758_Hebrew");
            audioDirectory.Add("C:\\Users\\Administrator\\Documents\\Visual Studio 2008\\Projects\\cmuspeechrecognition_cmuspeechmain\\test_data\\audio\\101410_140344_Hebrew");
            audioDirectory.Add("C:\\Users\\Administrator\\Documents\\Visual Studio 2008\\Projects\\cmuspeechrecognition_cmuspeechmain\\test_data\\audio\\101510_111237_Hebrew");
            // audio file names

            // audio file names
            List<string> audioFileNames = new List<string>();
            audioFileNames.Add("[Wed_(Sep_29_2010)_12-37-58]_4124143701_");
            audioFileNames.Add("[Thu_(Oct_14_2010)_14-03-44]_4122688595_");
            audioFileNames.Add("[Fri_(Oct_15_2010)_11-12-37]_4126203298_");

            GrammarReader gr = new GrammarReader("Hebrew", "C:\\Users\\Administrator\\Documents\\Visual Studio 2008\\Projects\\cmuspeechrecognition_cmuspeechmain\\test_data", "allcombinations");

            // set up data
            data = new Data(audioDirectory, audioFileNames, 5, speakersToBeTested, wordsToBeTested, samplesToBeTested,
               "C:\\Users\\Administrator\\Documents\\Visual Studio 2008\\Projects\\cmuspeechrecognition_cmuspeechmain\\test_data\\config_files\\config.txt.100.english");

            Tester t = new Tester(MSpeakers, MWordTypes, MSamples, data, directory,null,10, 1);
            t.runTestDiscriminative("multiList", new List<bool>(){true, true, true, true});
            //GrammarConverter gc = new GrammarConverter("Hebrew", "C:\\Users\\cheem\\Desktop\\SalaamTest", "allcombinations", 3, 5);
            //gc.convert();

        }
    }
}
