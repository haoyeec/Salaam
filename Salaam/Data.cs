using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Microsoft.Speech;

namespace Salaam
{
    public class Data
    {
        // audio files
        public List<string> audioDirectory { get; set; }
        public List<string> audioFileName { get; set; }

        // words
        public string language { get; set; }
        public int numberOfWords { get; set; }
        public int numberOfSamplesPerWord { get; set; }
        public List<int> wordsToBeTested { get; set; }
        public List<int> samplesToBeTested { get; set; }
        public List<string> listOfWords { get; set; }
        public int numberOfSpeakers { get; set; }
        public List<int> speakersToBeTested { get; set; }
        public string wordListPath { get; set; }


        public Data(List<string> audio_directory, List<string> audio_file_name, int number_of_samples_per_word, List<int> speakers_to_be_tested,
            List<int> words_to_be_tested, List<int> sample_to_be_tested, string word_list_path)
        {
            audioDirectory = audio_directory;
            audioFileName = audio_file_name;
            numberOfSamplesPerWord = number_of_samples_per_word;
            wordsToBeTested = words_to_be_tested;
            samplesToBeTested = sample_to_be_tested;
            readWordList(word_list_path);
            speakersToBeTested = speakers_to_be_tested;
            wordListPath = word_list_path;

            //if (audio_directory.Count != audio_file_name.Count || speakers_to_be_tested.Count != audio_file_name.Count)
            //{
             //   System.Diagnostics.Debug.WriteLine("Number of speakers do not tally with directory or file name");
              //  Environment.Exit(1);
           // }
            //else
           // {
                numberOfSpeakers = audio_directory.Count;
           // }
            
        }

        private void readWordList(string word_list_path)
        {
            string[] lines = File.ReadAllLines(word_list_path);
            language = lines[0];
            listOfWords = new List<string>();
            numberOfWords = 0;
            for (int i = 0; i < lines.Length; i++)
            {
                if (!lines[i].Trim().Equals(""))
                {
                    listOfWords.Add(lines[i]);
                    numberOfWords++;
                }
            }
            
        }

        public string getAudioName(int speaker_number, int word_number, int sample_number)
        {
            return audioDirectory[speaker_number] + "\\" + audioFileName[speaker_number] + word_number + "_" + sample_number + ".wav";
        }
    }
}
