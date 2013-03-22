using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Speech.Recognition;

namespace Salaam
{
    class Program
    {
        static Data data;
        static string directory = "C:\\Users\\cheem\\Desktop\\SalaamTest";

        static void Main(string[] args)
        {
                      
            // words to be tested
            List<int> wordsToBeTested = new List<int>();
            for(int i = 1; i <= 100; i++)
               wordsToBeTested.Add(i);
            //wordsToBeTested.Add(1);


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
            List<string> audioFileNames = new List<string>();
            audioFileNames.Add("[Wed_(Sep_29_2010)_12-37-58]_4124143701_");
            audioFileNames.Add("[Thu_(Oct_14_2010)_14-03-44]_4122688595_");
            audioFileNames.Add("[Fri_(Oct_15_2010)_11-12-37]_4126203298_");

            for (int i = 0; i < 3; i++)
            {
                List<int> speakersToBeTested = new List<int>();
                speakersToBeTested.Add(i);  
                // set up data
                data = new Data(audioDirectory, audioFileNames, 5, speakersToBeTested, wordsToBeTested, samplesToBeTested,
                    "C:\\Users\\Administrator\\Documents\\Visual Studio 2008\\Projects\\cmuspeechrecognition_cmuspeechmain\\test_data\\config_files\\config.txt.100.english");


                // setup grammar
                GrammarCreator gc = new GrammarCreator("C:\\Users\\Administrator\\Documents\\Visual Studio 2008\\Projects\\cmuspeechrecognition_cmuspeechmain\\test_data", "Hebrew", "allcombinations");

                // setup training
                TrainingAlgorithm ta = new TrainingAlgorithm(gc, data, 10, 15);

                ta.LearnAllWords();
            }
           
        }
    }
}
