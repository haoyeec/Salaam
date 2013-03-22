using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Salaam;
using TestSolutionMultipleSpeaker;

namespace DiscriminativeTraining
{
    class TrainingAlgorithm
    {
        // data for Tester
        private int numberOfAlternatives { get; set; }
        private Data data;
        private string directory;
        private int MWordTypes { get; set; }
        private int MSample { get; set; }
        private int MSpeakers { get; set; }
        private int startingNumberOfSamples = 5;
        private int repeats = 10;
        private string inputGrammarDirectory { get; set; }
        private double threshold = 0.7;
        private List<List<int>>[] trainingSet;
        private Dictionary<string, List<string>> pronunciations;  // stores all the possible alternative pronunciations
        private Tester tester;
        private string outputName;

        public class DescendingComparer<T> : IComparer<T> where T : IComparable<T>
        {
            public int Compare(T x, T y)
            {
                return y.CompareTo(x);
            }
        }

        public TrainingAlgorithm(int number_of_alternatives, Data d, string dir, int M_word_types, int M_sample, int M_speakers, string input_grammar_directory, int r, string output_name)
        {
            numberOfAlternatives = number_of_alternatives;
            data = d;
            directory = dir;
            MWordTypes = M_word_types;
            MSample = M_sample;
            MSpeakers = M_speakers;
            inputGrammarDirectory = input_grammar_directory;
            repeats = r;
            outputName = output_name;

        }


        public void trainWordsDiscriminativeEliminative()
        {
            // set up grammar files
            if (inputGrammarDirectory == null)
            {
                tester = new Tester(MSpeakers, MWordTypes, MSample, data, directory, null, numberOfAlternatives, 1);
                tester.testingAlgorithm();
                inputGrammarDirectory = directory;
            }




            GrammarCreator gc = null;
            // creating all grammars for every combination with 1 single pronunciation
            for (int NTrainingSpeakers = 1; NTrainingSpeakers < MSpeakers; NTrainingSpeakers++)
            {
                for (int NTrainingSamplesPerSpeaker = startingNumberOfSamples; NTrainingSamplesPerSpeaker <= MSample; NTrainingSamplesPerSpeaker++)
                {
                    setUpTrainingSet(NTrainingSpeakers, NTrainingSamplesPerSpeaker);

                    foreach (List<int> speakerList in trainingSet[0])
                    {
                        foreach (List<int> sampleList in trainingSet[1])
                        {
                            for (int i = 1; i <= MSpeakers; i++)
                            {
                                // Extract pronunciations
                                GrammarReader gr = new GrammarReader(data.language, inputGrammarDirectory, "allcombinations");
                                List<int> trueSampleList = new List<int>();
                                foreach (int sample in sampleList)
                                {
                                    trueSampleList.Add(sample + 1);
                                }
                                pronunciations = gr.output(speakerList, trueSampleList); // fill in all pronunciations
                                {
                                    // Create grammar with all alternative pronunciation
                                    string speaker = "speaker-";
                                    foreach (int speakerIndex in speakerList)
                                    {
                                        speaker += speakerIndex + "_";
                                    }
                                    string sample = "sample-";
                                    foreach (int sampleIndex in trueSampleList)
                                    {
                                        sample += sampleIndex + "_";

                                    }

                                    gc = new GrammarCreator(directory + "\\temp", data.language + speaker + sample + i, "allcombinations");
                                    gc.boundgr(true);
                                    gc.boundrule(true, "allcombinations");
                                    foreach (string wordType in pronunciations.Keys)
                                    {
                                        // use all alternative pronunciations
                                        for(int j = 0; j < pronunciations[wordType].Count; j++)
                                        {
                                            gc.LogGrammar("<item><token sapi:pron=\"" + pronunciations[wordType][j] + "\">" + wordType + "_" + j + "</token></item>");
                                        }
                                    }
                                    gc.boundrule(false, "");
                                    gc.boundgr(false);


                                }
                            }
                        }
                    }
                }

            }

            // begin testing
            // we want to create output numberOfAlternatives times but only analyse numberOfAlternatives - 1 times
            for (int currentNumberOfAlternatives = numberOfAlternatives; currentNumberOfAlternatives >= 1; currentNumberOfAlternatives--)
            {
                System.Diagnostics.Debug.WriteLine("currentNumberOfAlternatives: " + currentNumberOfAlternatives);
                // Test all combinations at one go
                GrammarReader grTemp = new GrammarReader(data.language, gc.grammarDir, "allcombinations");
                tester = new Tester(MSpeakers, MWordTypes, MSample, data, directory, grTemp, numberOfAlternatives, repeats);
                
                if(currentNumberOfAlternatives == 1)
                    tester.runTestDiscriminative(outputName, new List<bool>(){true, true, true,true});
                else
                    tester.runTestDiscriminative(outputName, new List<bool>() { true, true, false, true });
                // analyse all output at one go and make changes to individual grammar files if necessary
                
                analyseOutputDiscriminativeEliminative(currentNumberOfAlternatives);
             }




        }

        public bool analyseOutputDiscriminativeEliminative(int currentNumberOfAlternatives)
        {
            bool isCompleted = true;



            // analyse
            for (int NTrainingSpeakers = 1; NTrainingSpeakers < MSpeakers; NTrainingSpeakers++)
            {
                for (int NTrainingSamplesPerSpeaker = startingNumberOfSamples; NTrainingSamplesPerSpeaker <= MSample; NTrainingSamplesPerSpeaker++)
                {
                    for (int iTestSpeakerIndex = 0; iTestSpeakerIndex < MSpeakers; iTestSpeakerIndex++)
                    {
                       // for (int iWordTypeIndex = 0; iWordTypeIndex < MWordTypes; iWordTypeIndex++)
                       // {
                            // if fail test, change grammar
                           // if (tester.WordTypeAccuracy[NTrainingSpeakers - 1, 4, Tester.V.Length - 1, iTestSpeakerIndex, iWordTypeIndex].toDecimal() < threshold)
                           // {
                                isCompleted = false;

                                // extract the corresponding grammar files
                                List<List<int>> allFiles = Tester.choose(MSpeakers, NTrainingSpeakers);
                                List<List<int>> filesToChange = new List<List<int>>();
                                foreach (List<int> speakerList in allFiles)
                                {
                                    if (!speakerList.Contains(iTestSpeakerIndex))
                                    {
                                        filesToChange.Add(speakerList);
                                    }
                                }

                                System.Diagnostics.Debug.WriteLine("Files to be changed for testSpeaker: " + (iTestSpeakerIndex + 1).ToString());
                                foreach (List<int> speakerList in filesToChange)
                                {
                                    foreach (int speakerIndex in speakerList)
                                    {
                                        System.Diagnostics.Debug.Write(speakerIndex + ", ");
                                    }
                                    System.Diagnostics.Debug.WriteLine("");
                                }
                                // change the grammar file
                                foreach (List<int> speakerList in filesToChange)
                                {
                                    GrammarReader gr = new GrammarReader(data.language, directory + "\\temp", "allcombinations");
                                    List<int> sampleList = new List<int>();
                                    for (int i = 1; i <= MSample; i++)
                                    {
                                        sampleList.Add(i);
                                    }
                                    
                                    // get the current pronunciations
                                    Dictionary<string, List<string>> currentPronun = gr.outputDiscriminativeEliminative(speakerList, sampleList, iTestSpeakerIndex + 1);

                                    // pronunciations
                                    Dictionary<string, SortedList<double,List<Dictionary<string, Fraction[]>>>> mainPronunTable = new Dictionary<string, SortedList<double, List<Dictionary<string, Fraction[]>>>>();

                                    // fill in pronunciations
                                    foreach (string wordType in currentPronun.Keys)
                                    {
                                        mainPronunTable[wordType] = new SortedList<double, List<Dictionary<string, Fraction[]>>>(new DescendingComparer<double>());
                                        
                                        foreach (string pronunciation in currentPronun[wordType])
                                        {
                                            // create dictionary
                                            Dictionary<string, Fraction[]> actualPronun = new Dictionary<string, Fraction[]>();
                                            string[] pronun = pronunciation.Split('_');

                                            // calculate shyness and eagerness
                                            Fraction shyness = new Fraction();
                                            Fraction eagerness = new Fraction();
                                            // summing over every row
                                            int wordTypeIndex = Array.IndexOf(data.listOfWords.ToArray(), wordType) - 1;
                                            for (int iRow = 0; iRow < MSample * MWordTypes; iRow++)
                                            {
                                                
                                                if ((iRow / MSample) == wordTypeIndex)
                                                {
                                                    shyness = shyness + tester.ConfusionMatrix[NTrainingSpeakers - 1, iTestSpeakerIndex, iRow, numberOfAlternatives * wordTypeIndex + int.Parse(pronun[1])];
                                                }
                                                else
                                                {
                                                    eagerness = eagerness + tester.ConfusionMatrix[NTrainingSpeakers - 1, iTestSpeakerIndex, iRow, numberOfAlternatives * wordTypeIndex + int.Parse(pronun[1])];
                                                }
                                            }

                                           
                                            Fraction[] errors = new Fraction[2];
                                            errors[0] = eagerness;
                                            errors[1] = shyness;

                                            actualPronun.Add(pronunciation, errors);
                                            
                                            // calculate key

                                            double key = eagerness.toDecimal();
                                            List<Dictionary<string, Fraction[]>> entry;
                                            if (mainPronunTable[wordType].ContainsKey(key))
                                            {
                                                entry = mainPronunTable[wordType][key];
                                                entry.Add(actualPronun);
                                                mainPronunTable[wordType].Remove(key);
                                                mainPronunTable[wordType].Add(key, entry);
                                            }
                                            else
                                            {
                                                entry = new List<Dictionary<string, Fraction[]>>();
                                                entry.Add(actualPronun);
                                                mainPronunTable[wordType].Add(key, entry);
                                            }
                                           

                                            
                                        }


                                    }

                                   

                                    // create the new grammar file

                                    string speaker = "speaker-";
                                    foreach (int speakerIndex in speakerList)
                                    {
                                        speaker += speakerIndex + "_";
                                    }
                                    string sample = "sample-";
                                    foreach (int sampleIndex in sampleList)
                                    {
                                        sample += sampleIndex + "_";

                                    }

                                    GrammarCreator gc = new GrammarCreator(directory + "\\temp", data.language + speaker + sample + (iTestSpeakerIndex + 1).ToString(), "allcombinations");

                                    gc.boundgr(true);
                                    gc.boundrule(true, gc.grammarRuleName);

                                    // add the words
                                    foreach (string wordType in mainPronunTable.Keys)
                                    {
                                        
                                        Boolean removed = false;
                                        SortedList<double, List<Dictionary<string, Fraction[]>>> entry = mainPronunTable[wordType];
                                        for (int i = 0; i < entry.Count; i++)
                                        {

                                            if (entry.Keys[i] > 0 && !removed && entry.Values.Count > 1)
                                            {
                                                System.Diagnostics.Debug.WriteLine("outside: " + wordType);
                                                System.Diagnostics.Debug.WriteLine("skipping file: " + directory + "\\temp\\" + data.language + speaker + sample + "_" + wordType + "_" + (iTestSpeakerIndex + 1).ToString());
                                                removed = true;
                                                continue;
                                            }
                                            //if()
                                            //{
                                            foreach (Dictionary<string, Fraction[]> listOfPronunciations in entry.Values[i])
                                            {

                                                foreach (string pronunciation in listOfPronunciations.Keys)
                                                {
                                                    string[] pronun = pronunciation.Split('_');
                                                    // remove pronunciations that are not useful
                                                    /*if (!removed && listOfPronunciations[pronunciation][1].num == 0 && entry.Values[i].Count > 1)
                                                    {
                                                        System.Diagnostics.Debug.WriteLine("inside: " + wordType + " " + pronunciation);
                                                        System.Diagnostics.Debug.WriteLine("skipping file: " + directory + "\\temp\\" + data.language + speaker + sample + "_" + wordType + "_" + (iTestSpeakerIndex + 1).ToString());
                                                        removed = true;
                                                        continue;
                                                    }*/

                                                    gc.LogGrammar("<item><token sapi:pron=\"" + pronun[0] + "\">" + wordType + "_" + int.Parse(pronun[1]) + "</token></item>");

                                                }
                                            }
                                            //}


                                        }
                                        
                                    }
                                    gc.boundrule(false, "");
                                    gc.boundgr(false);


                                }
                            //}
                        //}
                    }

                }
            }

            return isCompleted;
        }
        public void trainWordsDiscriminativeNaive()
        {
            // set up grammar files
            if (inputGrammarDirectory == null)
            {
                tester = new Tester(MSpeakers, MWordTypes, MSample, data, directory, null, numberOfAlternatives, 1);
                tester.testingAlgorithm();
                inputGrammarDirectory = directory;
            }

           


            GrammarCreator gc = null;
            // creating all grammars for every combination with 1 single pronunciation
            for (int NTrainingSpeakers = 1; NTrainingSpeakers < MSpeakers; NTrainingSpeakers++)
            {
                for (int NTrainingSamplesPerSpeaker = startingNumberOfSamples; NTrainingSamplesPerSpeaker <= MSample; NTrainingSamplesPerSpeaker++)
                {
                    setUpTrainingSet(NTrainingSpeakers, NTrainingSamplesPerSpeaker);

                    foreach (List<int> speakerList in trainingSet[0])
                    {
                        foreach (List<int> sampleList in trainingSet[1])
                        {
                            for (int i = 1; i <= MSpeakers; i++)
                            {
                                // Extract pronunciations
                                GrammarReader gr = new GrammarReader(data.language, inputGrammarDirectory, "allcombinations");
                                List<int> trueSampleList = new List<int>();
                                foreach (int sample in sampleList)
                                {
                                    trueSampleList.Add(sample + 1);
                                }
                                pronunciations = gr.output(speakerList, trueSampleList); // fill in all pronunciations
                                {
                                    // Create grammar with 1 alternative pronunciation
                                    string speaker = "speaker-";
                                    foreach (int speakerIndex in speakerList)
                                    {
                                        speaker += speakerIndex + "_";
                                    }
                                    string sample = "sample-";
                                    foreach (int sampleIndex in trueSampleList)
                                    {
                                        sample += sampleIndex + "_";

                                    }

                                    gc = new GrammarCreator(directory + "\\temp", data.language + speaker + sample + i, "allcombinations");
                                    gc.boundgr(true);
                                    gc.boundrule(true, "allcombinations");
                                    foreach (string wordType in pronunciations.Keys)
                                    {
                                        // we only use the first one here to setup the initial pass
                                        gc.LogGrammar("<item><token sapi:pron=\"" + pronunciations[wordType][0] + "\">" + wordType + "_0" + "</token></item>");

                                    }
                                    gc.boundrule(false, "");
                                    gc.boundgr(false);


                                }
                            }
                        }
                    }
                }

            }

            // begin testing
            for (int currentNumberOfAlternatives = 1; currentNumberOfAlternatives <= numberOfAlternatives; currentNumberOfAlternatives++)
            {
                
                // Test all combinations at one go
                GrammarReader grTemp = new GrammarReader(data.language, gc.grammarDir, "allcombinations");
                tester = new Tester(MSpeakers, MWordTypes, MSample, data, directory, grTemp, numberOfAlternatives, repeats);
                tester.runTestDiscriminative(outputName, new List<bool>(){true, true, true, true});
                // analyse all output at one go and make changes to individual grammar files if necessary
                // if we are done, break out
                if (analyseOutputDiscriminativeNaive(currentNumberOfAlternatives)) break;
            }
            
            
            
            
        }

        public bool analyseOutputDiscriminativeNaive(int currentNumberOfAlternatives)
        {
            bool isCompleted = true;



            // analyse
            for (int NTrainingSpeakers = 1; NTrainingSpeakers < MSpeakers; NTrainingSpeakers++)
            {
                for (int NTrainingSamplesPerSpeaker = startingNumberOfSamples; NTrainingSamplesPerSpeaker <= MSample; NTrainingSamplesPerSpeaker++)
                {
                   for (int iTestSpeakerIndex = 0; iTestSpeakerIndex < MSpeakers; iTestSpeakerIndex++)
                   {
                       for (int iWordTypeIndex = 0; iWordTypeIndex < MWordTypes; iWordTypeIndex++)
                       {
                           // if fail test, change grammar
                           if (tester.WordTypeAccuracy[NTrainingSpeakers - 1, 4, Tester.V.Length - 1, iTestSpeakerIndex, iWordTypeIndex].toDecimal() < threshold)
                           {
                               isCompleted = false;

                               // extract the corresponding grammar files
                               List<List<int>> allFiles = Tester.choose(MSpeakers, NTrainingSpeakers);
                               List<List<int>> filesToChange = new List<List<int>>();
                               foreach (List<int> speakerList in allFiles)
                               {
                                   if (!speakerList.Contains(iTestSpeakerIndex))
                                   {
                                       filesToChange.Add(speakerList);
                                   }
                               }
                                   
                               // change the grammar file
                               foreach (List<int> speakerList in filesToChange)
                               {
                                   GrammarReader gr = new GrammarReader(data.language, directory + "\\temp", "allcombinations");
                                   List<int> sampleList = new List<int>();
                                   for (int i = 1; i <= MSample; i++)
                                   {
                                       sampleList.Add(i);
                                   }

                                   // get the current pronunciations
                                   Dictionary<string, List<string>> currentPronun = gr.outputDiscriminative(speakerList, sampleList,iTestSpeakerIndex + 1);

                                   // if there are less alternative pronunciations than what we need, we do not add
                                   if (pronunciations[data.listOfWords[iWordTypeIndex + 1]].Count <= currentNumberOfAlternatives)
                                       continue;

                                   currentPronun[data.listOfWords[iWordTypeIndex + 1]].Add(pronunciations[data.listOfWords[iWordTypeIndex + 1]][currentNumberOfAlternatives]);
                                   
                                   // create the new grammar file

                                   string speaker = "speaker-";
                                   foreach (int speakerIndex in speakerList)
                                   {
                                       speaker += speakerIndex + "_";
                                   }
                                   string sample = "sample-";
                                   foreach (int sampleIndex in sampleList)
                                   {
                                       sample += sampleIndex + "_";

                                   }

                                   GrammarCreator gc = new GrammarCreator(directory + "\\temp", data.language + speaker + sample + (iTestSpeakerIndex + 1).ToString(), "allcombinations");
                                   
                                   gc.boundgr(true);
                                   gc.boundrule(true, gc.grammarRuleName);

                                   // add the words
                                   foreach (string wordType in currentPronun.Keys)
                                   {
                                       for (int i = 0; i < currentPronun[wordType].Count; i++)
                                       {

                                           gc.LogGrammar("<item><token sapi:pron=\"" + currentPronun[wordType][i] + "\">" + wordType + "_" + i + "</token></item>");
                                           System.Diagnostics.Debug.WriteLine("Changing file: " + directory + "\\temp", data.language + speaker + sample + "_" + wordType + "_" + i);
                                           
                                       }
                                   }
                                   gc.boundrule(false, "");
                                   gc.boundgr(false);


                               }
                           }
                       }
                   }

                }
            }

            return isCompleted;
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
