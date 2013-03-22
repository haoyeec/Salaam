using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Salaam;

namespace TestSolutionMultipleSpeaker
{
    class GrammarConverter
    {
        public string language { get; set; }
        public string directory { get; set; }
        public string ruleName { get; set; }
        public int MSpeakers { get; set; }
        public int MSample { get; set; }
        private int startingNumberOfSamples = 5;
        private GrammarReader gr;
        private GrammarCreator gc;
        private List<List<int>>[] trainingSet;
        public GrammarConverter(string lang, string dir, string rule_name, int M_speakers, int M_sample)
        {
            language = lang;
            directory = dir;
            ruleName = rule_name;
            MSpeakers = M_speakers;
            MSample = M_sample;
            gr = new GrammarReader(language, directory, ruleName);

        }

        public void convert()
        {
            for (int NTrainingSpeakers = 1; NTrainingSpeakers < MSpeakers; NTrainingSpeakers++)
            {
                for (int NTrainingSamplesPerSpeaker = startingNumberOfSamples; NTrainingSamplesPerSpeaker <= MSample; NTrainingSamplesPerSpeaker++)
                {
                    setUpTrainingSet(NTrainingSpeakers, NTrainingSamplesPerSpeaker);
                    foreach (List<int> speakerList in trainingSet[0])
                    {
                        foreach (List<int> sampleList in trainingSet[1])
                        {
                            List<int> trueSampleList = new List<int>();
                            foreach (int index in sampleList)
                            {
                                trueSampleList.Add(index + 1);
                            }
                            gc = new GrammarCreator(directory, gr.getFileName(speakerList, trueSampleList), "allcombinations");
                            Dictionary<string, List<string>> pronunciations = gr.output(speakerList, trueSampleList);
                            gc.boundgr(true);
                            gc.boundrule(true, gc.grammarRuleName);
                            foreach (string wordType in pronunciations.Keys)
                            {
                                for (int i = 0; i < pronunciations[wordType].Count; i++)
                                {
                                    gc.LogGrammar("<item><token sapi:pron=\"" + pronunciations[wordType][i] + "\">" + wordType + "_" + i + "</token></item>");
                                }
                            }
                            gc.boundrule(false, "");
                            gc.boundgr(false);
                            
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Given number of speakers and samples, this method populates the training sets
        /// </summary>
        /// <param name="number_of_speakers">number of speakers</param>
        /// <param name="number_of_samples">number of samples</param>
        private void setUpTrainingSet(int number_of_speakers, int number_of_samples)
        {
            // set up training set
            trainingSet = new List<List<int>>[2];
            for (int i = 0; i < trainingSet.Length; i++)
            {
                trainingSet[i] = new List<List<int>>();
            }

            // initialize training set
            trainingSet[0].AddRange(Tester.choose(MSpeakers, number_of_speakers));
            trainingSet[1].AddRange(Tester.choose(MSample, number_of_samples));

        }
    }
}
