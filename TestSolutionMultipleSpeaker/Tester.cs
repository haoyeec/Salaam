using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Salaam;
using Microsoft.Speech.Recognition;
using System.IO;
using System.Collections;
namespace TestSolutionMultipleSpeaker
{
    public class Tester
    {
        // parameters
        
        private int MWordTypes { get; set; }
        private int MSample { get; set; }
        public static int[] V = {100};
        private int MSpeakers { get; set; }
        private int startingNumberOfSamples = 5;
        private int repeats = 10;
        private double threshold = 0.01;
        private GrammarReader input { get; set; }
        private int numberOfAlternates { get; set; }
        private int typesOfErrors = 2; // shyness and eagerness, can be changed if more different types of errors are found

        // data
        private Data data { get; set; }
        private List<List<int>>[] trainingSet;

        // output
        public string grammarDirectory;
        public Fraction[, , , , ,] SampleAccuracy;
        public Fraction[, , , ,] WordTypeAccuracy;
        public Fraction[, , ,] OverallWordTypeAccuracy;
        public Fraction[, ,] OverallAccuracy;
        public List<int>[, , ,] SampleDeviation;
        public Fraction[, , ,] ConfusionMatrix;
        public Fraction[, , ,] ErrorRates;

        // personal debugging
        private int grammarCounter = 0;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="M_speakers">Number of speakers</param>
        /// <param name="M_word_types">Number of word types</param>
        /// <param name="M_sample">Number of samples per word type per speaker</param>
        /// <param name="d">audio data</param>
        /// <param name="grammar_directory">output directory of created grammars</param>
        /// <param name="input">grammar file input</param>
        /// <param name="number_of_alternates">number of alternative pronunciations</param>
        /// <param name="r">number of repeats</param>
        public Tester(int M_speakers, int M_word_types, int M_sample, Data d, string grammar_directory, GrammarReader input, int number_of_alternates, int r)
        {
            MWordTypes = M_word_types;
            MSample = M_sample;
            MSpeakers = M_speakers;
            data = d;
            grammarDirectory = grammar_directory;
            this.input = input;
            numberOfAlternates = number_of_alternates;
            repeats = r;
            // initialize output
            SampleAccuracy = new Fraction[MSpeakers - 1, MSample, V.Length, MSpeakers, MWordTypes, MSample];
            WordTypeAccuracy = new Fraction[MSpeakers - 1, MSample, V.Length, MSpeakers, MWordTypes];
            OverallWordTypeAccuracy = new Fraction[MSpeakers - 1, MSample, V.Length, MWordTypes];
            OverallAccuracy = new Fraction[MSpeakers-1, MSample, V.Length];
            SampleDeviation = new List<int>[MSpeakers-1,MSample, MSpeakers, MWordTypes];
            ConfusionMatrix = new Fraction[MSpeakers - 1, MSpeakers, MWordTypes * MSample, (numberOfAlternates * MWordTypes) + 1];
            ErrorRates = new Fraction[MSpeakers - 1, MSpeakers, typesOfErrors, (numberOfAlternates * MWordTypes)];

            for (int NTrainingSpeakersIndex = 0; NTrainingSpeakersIndex < MSpeakers - 1; NTrainingSpeakersIndex++)
            {
                for (int NTrainingSamplesPerSpeakerIndex = 0; NTrainingSamplesPerSpeakerIndex < MSample; NTrainingSamplesPerSpeakerIndex++)
                {
                    for (int vocabSizeIndex = 0; vocabSizeIndex < V.Length; vocabSizeIndex++)
                    {
                        OverallAccuracy[NTrainingSpeakersIndex, NTrainingSamplesPerSpeakerIndex, vocabSizeIndex] = new Fraction();
                        for (int iTestSpeakerIndex = 0; iTestSpeakerIndex < MSpeakers; iTestSpeakerIndex++)
                        {
                            for (int iWordTypeIndex = 0; iWordTypeIndex < MWordTypes; iWordTypeIndex++)
                            {
                                WordTypeAccuracy[NTrainingSpeakersIndex, NTrainingSamplesPerSpeakerIndex, vocabSizeIndex, iTestSpeakerIndex, iWordTypeIndex] = new Fraction();
                                OverallWordTypeAccuracy[NTrainingSpeakersIndex, NTrainingSamplesPerSpeakerIndex, vocabSizeIndex, iWordTypeIndex] = new Fraction();
                                SampleDeviation[NTrainingSpeakersIndex, NTrainingSamplesPerSpeakerIndex, iTestSpeakerIndex, iWordTypeIndex] = new List<int>();
                                for (int iSamplesIndex = 0; iSamplesIndex < MSample; iSamplesIndex++)
                                {
                                    SampleAccuracy[NTrainingSpeakersIndex, NTrainingSamplesPerSpeakerIndex, vocabSizeIndex, iTestSpeakerIndex, iWordTypeIndex, iSamplesIndex] = new Fraction();
                                }
                            }
                        }
                        
                    }
                }
            }
            for (int NTrainingSpeakersIndex = 0; NTrainingSpeakersIndex < MSpeakers - 1; NTrainingSpeakersIndex++)
            {
                for (int iTestSpeakerIndex = 0; iTestSpeakerIndex < MSpeakers; iTestSpeakerIndex++)
                {
                    for (int i = 0; i < MSample*MWordTypes; i++)
                    {
                        for (int j = 0; j < (numberOfAlternates*MWordTypes) + 1; j++)
                        {
                            ConfusionMatrix[NTrainingSpeakersIndex, iTestSpeakerIndex, i, j] = new Fraction();
                            if ( i < typesOfErrors && j < numberOfAlternates*MWordTypes)
                                ErrorRates[NTrainingSpeakersIndex, iTestSpeakerIndex, i, j] = new Fraction();
                        }
                    }
                }
            }
        }

        /**************************************************************************************/
        /*
        /* Testing algorithms
        /*
        /***************************************************************************************/


        /// <summary>
        /// Runs the testing algorithm
        /// </summary>
        public void testingAlgorithmDiscriminative()
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
                            Dictionary<string, List<string>> pronunciations;
                            /*if (input == null)
                            {
                                pronunciations = trainAllWords(speakerList, sampleList);
                            }
                            else
                            {
                                List<int> trueSampleList = new List<int>();
                                foreach (int sample in sampleList)
                                {
                                    trueSampleList.Add(sample + 1);
                                }
                                pronunciations = input.output(speakerList, trueSampleList);
                                System.Diagnostics.Debug.WriteLine(pronunciations.Count);


                            }*/

                            // set up testSpeakerSet
                            List<int> testSpeakerSet = new List<int>();
                            for (int speakerNum = 0; speakerNum < MSpeakers; speakerNum++)
                            {
                                if (!speakerList.Contains(speakerNum))
                                {
                                    testSpeakerSet.Add(speakerNum);
                                }

                            }

                            // begin testing
                            foreach (int vocabSize in V)
                            {
                                foreach (int iTestSpeaker in testSpeakerSet)
                                {
                                    if (input == null)
                                    {
                                        pronunciations = trainAllWords(speakerList, sampleList);
                                    }
                                    else
                                    {
                                        List<int> trueSampleList = new List<int>();
                                        foreach (int sample in sampleList)
                                        {
                                            trueSampleList.Add(sample + 1);
                                        }
                                        pronunciations = input.outputDiscriminative(speakerList, trueSampleList, (iTestSpeaker+ 1));
                                        System.Diagnostics.Debug.WriteLine(pronunciations.Count);


                                    }
                                    for (int iWordType = 1; iWordType <= MWordTypes; iWordType++)
                                    {
                                        System.Diagnostics.Debug.WriteLine("Testing: word" + iWordType); 
                                        for (int iRepeat = 1; iRepeat <= repeats; iRepeat++)
                                        {
                                            // create random vocab
                                            GrammarCreator gc = new GrammarCreator(grammarDirectory, "repeat_" + grammarCounter, "allcombinations");
                                            grammarCounter++;
                                            gc.boundgr(true);
                                            gc.boundrule(true, gc.grammarRuleName);

                                            // add correct word
                                            for (int i = 0; i < pronunciations[data.listOfWords[iWordType]].Count; i++)
                                            {
                                                for (int j = 0; j < MSample; j++)
                                                {
                                                    ConfusionMatrix[NTrainingSpeakers - 1, iTestSpeaker, MSample * (iWordType - 1) + j, numberOfAlternates * (iWordType - 1) + i].den++;
                                                }
                                                gc.LogGrammar("<item><token sapi:pron=\"" + pronunciations[data.listOfWords[iWordType]][i] + "\">" + data.listOfWords[iWordType] + "_" + i + "</token></item>");
                                            }
                                            /*foreach (string pronun in pronunciations[data.listOfWords[iWordType]])
                                            {
                                                gc.LogGrammar("<item><token sapi:pron=\"" + pronun + "\">" + data.listOfWords[iWordType] + "</token></item>");
                                            }*/


                                            // add V - 1 random words
                                            Random random = new Random();
                                            List<int> wordTypesChosen = new List<int>();
                                            wordTypesChosen.Add(iWordType);

                                            //ConfusionMatrix[NTrainingSpeakers - 1, iTestSpeaker, iWordType - 1, iWordType - 1].den += MSample;


                                            int wordCount = 1;

                                            while (wordCount < vocabSize)
                                            {
                                                int randomWordType;
                                                if (vocabSize < MWordTypes)
                                                {
                                                    
                                                    do
                                                    {
                                                        randomWordType = random.Next(1, MWordTypes + 1); // 1 to MWordTypes
                                                    } while (wordTypesChosen.Contains(randomWordType));
                                                    // add word
                                                    wordTypesChosen.Add(randomWordType);
                                                    for (int i = 0; i < pronunciations[data.listOfWords[randomWordType]].Count; i++)
                                                    {
                                                        for (int j = 0; j < MSample; j++)
                                                        {

                                                            ConfusionMatrix[NTrainingSpeakers - 1, iTestSpeaker, MSample * (iWordType - 1) + j, numberOfAlternates * (randomWordType - 1) + i].den++;
                                                        }
                                                        gc.LogGrammar("<item><token sapi:pron=\"" + pronunciations[data.listOfWords[randomWordType]][i] + "\">" + data.listOfWords[randomWordType] + "_" + i + "</token></item>");
                                                    }
                                                    wordCount++;
                                                }
                                                else
                                                {
                                                    for (int k = 1; k <= MWordTypes; k++)
                                                    {
                                                        randomWordType = k;
                                                        // we don't add the "correct" word
                                                        if (randomWordType != iWordType)
                                                        {
                                                            wordTypesChosen.Add(randomWordType);
                                                            for (int i = 0; i < pronunciations[data.listOfWords[randomWordType]].Count; i++)
                                                            {
                                                                for (int j = 0; j < MSample; j++)
                                                                {

                                                                    ConfusionMatrix[NTrainingSpeakers - 1, iTestSpeaker, MSample * (iWordType - 1) + j, numberOfAlternates * (randomWordType - 1) + i].den++;
                                                                }
                                                                gc.LogGrammar("<item><token sapi:pron=\"" + pronunciations[data.listOfWords[randomWordType]][i] + "\">" + data.listOfWords[randomWordType] + "_" + i + "</token></item>");
                                                            }
                                                            //System.Diagnostics.Debug.WriteLine("word chosen: " + randomWordType);
                                                           

                                                        }
                                                        
                                                    }
                                                    
                                                    break;
                                                }
                                                //ConfusionMatrix[NTrainingSpeakers - 1, iTestSpeaker, iWordType - 1, randomWordType - 1].den+=MSample;

                                                

                                                /*foreach (string pronun in pronunciations[data.listOfWords[randomWordType]])
                                                {
                                                    gc.LogGrammar("<item><token sapi:pron=\"" + pronun + "\">" + data.listOfWords[randomWordType] + "</token></item>");
                                                }*/

                                                //wordCount++;
                                            }
                                            gc.boundrule(false, "");
                                            gc.boundgr(false);
                                            
                                            // setup recognizer
                                            SpeechRecognitionEngine rec = new SpeechRecognitionEngine();
                                            rec.LoadGrammar(gc.getGrammar());

                                            // recognizing
                                            for (int iSample = 1; iSample <= MSample; iSample++)
                                            {
                                                rec.SetInputToWaveFile(data.getAudioName(iTestSpeaker, iWordType, iSample));
                                                RecognitionResult result;
                                                try
                                                {
                                                    result = rec.Recognize();
                                                }
                                                catch (Exception e)
                                                {
                                                    System.Diagnostics.Debug.WriteLine(e.Message);
                                                    System.Diagnostics.Debug.WriteLine(data.getAudioName(iTestSpeaker, iWordType, iSample));
                                                    continue;
                                                }
                                                //System.Diagnostics.Debug.WriteLine("recognising");
                                                if (result == null)
                                                {
                                                    ConfusionMatrix[NTrainingSpeakers - 1, iTestSpeaker, MSample * (iWordType - 1) + iSample - 1, numberOfAlternates * MWordTypes].num++;
                                                }
                                                else
                                                {
                                                    string[] word = result.Alternates[0].Words[0].Text.Split('_');
                                                    word = wordTypeResult(2, result);
                                                    //if (result.Alternates.Count > 2)
                                                     //   System.Diagnostics.Debug.WriteLine("No. of results found for wordType " + iWordType + "sample " + iSample + " = " + result.Alternates[2].Confidence);
                                                    ConfusionMatrix[NTrainingSpeakers - 1, iTestSpeaker, MSample * (iWordType - 1) + iSample - 1, numberOfAlternates * (Array.IndexOf(data.listOfWords.ToArray(), word[0]) - 1) + int.Parse(word[1])].num++;

                                                    if (word[0].Equals(data.listOfWords[iWordType]))
                                                        SampleAccuracy[NTrainingSpeakers - 1, NTrainingSamplesPerSpeaker - 1, Array.IndexOf(V, vocabSize), iTestSpeaker, iWordType - 1, iSample - 1].num++;

                                                }
                                                /*
                                                if (result != null && result.Alternates[0].Words[0].Text.Split('_')[0].Equals(data.listOfWords[iWordType]))
                                                {
                                                    SampleAccuracy[NTrainingSpeakers - 1, NTrainingSamplesPerSpeaker - 1, Array.IndexOf(V, vocabSize), iTestSpeaker, iWordType - 1, iSample - 1].num++;
                                                    //System.Diagnostics.Debug.WriteLine(NTrainingSpeakers + "_" + NTrainingSamplesPerSpeaker + "_" + Array.IndexOf(V, vocabSize) + "_" + iTestSpeaker + "_" + iWordType + "_" + iSample);
                                                }*/
                                                SampleAccuracy[NTrainingSpeakers - 1, NTrainingSamplesPerSpeaker - 1, Array.IndexOf(V, vocabSize), iTestSpeaker, iWordType - 1, iSample - 1].den++;
                                               
                                            }

                                            gc.Destroy();
                                        }
                                    }
                                }
                            }


                            // end testing
                        }
                    }
                }
            }
        }

        /// <summary>
        /// wordTypeResult returns the alternative pronunciation deemed best by some method
        /// </summary>
        /// <param name="method">Method to decide the best pronunciation</param>
        /// <param name="result">Recognition result</param>
        /// <returns>best result</returns>
        private String[] wordTypeResult(int method, RecognitionResult result)
        {
            String[] output = null;
            switch (method)
            {
                case(0): 
                {
                    output = result.Alternates[0].Words[0].Text.Split('_');
                    break;
                }
                case (1):
                {
                    Double count = 1;
                    Double alpha = 2;
                    
                    Dictionary<String, Double> scoreTable = new Dictionary<String, Double>();
                    
                    // Updating the results using the rank formula: 1/(r^alpha)
                    foreach (RecognizedPhrase altPhrase in result.Alternates)
                    {
                        String altPronun = altPhrase.Words[0].Text;
                        Double score = 1.0 / (Math.Pow(count, alpha));
                        Double tempScore;
                        if (scoreTable.ContainsKey(altPronun))
                        {
                            tempScore = scoreTable[altPronun] + score;
                            scoreTable.Remove(altPronun);
                        }
                        else
                        {
                            tempScore = score;
                        }
                        scoreTable.Add(altPronun, tempScore);
                    }

                    // Sorting and returning the best result
                    String[] values = new String[scoreTable.Count];
                    Double[] keys = new Double[scoreTable.Count];
                    scoreTable.Values.CopyTo(keys, 0);
                    scoreTable.Keys.CopyTo(values, 0);
                    Array.Sort(keys, values);
                    output = values[scoreTable.Count - 1].Split('_');
                    break;
                }
                case (2):
                {
                    Dictionary<String, Double> scoreTable = new Dictionary<String, Double>();

                    // Updating the results using the rank formula: 1/(r^alpha)
                    foreach (RecognizedPhrase altPhrase in result.Alternates)
                    {
                        String altPronun = altPhrase.Words[0].Text;
                        Double score = altPhrase.Words[0].Confidence;
                        Double tempScore;
                        if (scoreTable.ContainsKey(altPronun))
                        {
                            tempScore = scoreTable[altPronun] + score;
                            scoreTable.Remove(altPronun);
                        }
                        else
                        {
                            tempScore = score;
                        }
                        scoreTable.Add(altPronun, tempScore);
                    }

                    // Sorting and returning the best result
                    String[] values = new String[scoreTable.Count];
                    Double[] keys = new Double[scoreTable.Count];
                    scoreTable.Values.CopyTo(keys, 0);
                    scoreTable.Keys.CopyTo(values, 0);
                    Array.Sort(keys, values);
                    output = values[scoreTable.Count - 1].Split('_');
                    break;
                }
                default:
                throw new Exception("Tester.wordTypeResult: wrong case\n");

            }
            return output;
        }

        /// <summary>
        /// Runs the testing algorithm
        /// </summary>
        public void testingAlgorithm()
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
                            Dictionary<string, List<string>> pronunciations;
                            if (input == null)
                            {
                                pronunciations = trainAllWords(speakerList, sampleList);
                            }
                            else
                            {
                                List<int> trueSampleList = new List<int>();
                                foreach (int sample in sampleList)
                                {
                                    trueSampleList.Add(sample + 1);
                                }
                                pronunciations = input.output(speakerList, trueSampleList);
                                System.Diagnostics.Debug.WriteLine(pronunciations.Count);
                                
                                
                            }
                            // set up testSpeakerSet
                            List<int> testSpeakerSet = new List<int>();
                            for (int speakerNum = 0; speakerNum < MSpeakers; speakerNum++)
                            {
                                if(!speakerList.Contains(speakerNum))
                                {
                                    testSpeakerSet.Add(speakerNum);                                    
                                }

                            }

                            // begin testing
                            foreach (int vocabSize in V)
                            {
                                foreach (int iTestSpeaker in testSpeakerSet)
                                {
                                    for (int iWordType = 1; iWordType <= MWordTypes; iWordType++)
                                    {
                                        
                                        for (int iRepeat = 1; iRepeat <= repeats; iRepeat++)
                                        {
                                            // create random vocab
                                            GrammarCreator gc = new GrammarCreator(grammarDirectory,"repeat_"+grammarCounter, "allcombinations");
                                            grammarCounter++;
                                            gc.boundgr(true);
                                            gc.boundrule(true, gc.grammarRuleName);

                                            // add correct word
                                            for (int i = 0; i < pronunciations[data.listOfWords[iWordType]].Count; i++)
                                            {
                                                for (int j = 0; j < MSample; j++)
                                                {
                                                    ConfusionMatrix[NTrainingSpeakers - 1, iTestSpeaker, MSample * (iWordType - 1) + j, numberOfAlternates * (iWordType - 1) + i].den++;
                                                }
                                                gc.LogGrammar("<item><token sapi:pron=\"" + pronunciations[data.listOfWords[iWordType]][i] + "\">" + data.listOfWords[iWordType] + "_" + i + "</token></item>");
                                            }
                                            /*foreach (string pronun in pronunciations[data.listOfWords[iWordType]])
                                            {
                                                gc.LogGrammar("<item><token sapi:pron=\"" + pronun + "\">" + data.listOfWords[iWordType] + "</token></item>");
                                            }*/
                                            
                                          
                                            // add V - 1 random words
                                            Random random = new Random();
                                            List<int> wordTypesChosen = new List<int>();
                                            wordTypesChosen.Add(iWordType);

                                            //ConfusionMatrix[NTrainingSpeakers - 1, iTestSpeaker, iWordType - 1, iWordType - 1].den += MSample;
                                            
                                            
                                            int wordCount = 1;
                                            
                                            while (wordCount < vocabSize)
                                            {

                                                int randomWordType;
                                                do
                                                {
                                                    randomWordType = random.Next(1,MWordTypes+1); // 1 to MWordTypes
                                                } while(wordTypesChosen.Contains(randomWordType));
                                                // add word
                                                wordTypesChosen.Add(randomWordType);
                                                //ConfusionMatrix[NTrainingSpeakers - 1, iTestSpeaker, iWordType - 1, randomWordType - 1].den+=MSample;

                                                for (int i = 0; i < pronunciations[data.listOfWords[randomWordType]].Count; i++)
                                                {
                                                    for (int j = 0; j < MSample; j++)
                                                    {
                                                        
                                                        ConfusionMatrix[NTrainingSpeakers - 1, iTestSpeaker, MSample * (iWordType - 1) + j, numberOfAlternates * (randomWordType - 1) + i].den++;
                                                    }
                                                    gc.LogGrammar("<item><token sapi:pron=\"" + pronunciations[data.listOfWords[randomWordType]][i] + "\">" + data.listOfWords[randomWordType] + "_" + i + "</token></item>");
                                                }
                                                
                                                /*foreach (string pronun in pronunciations[data.listOfWords[randomWordType]])
                                                {
                                                    gc.LogGrammar("<item><token sapi:pron=\"" + pronun + "\">" + data.listOfWords[randomWordType] + "</token></item>");
                                                }*/
                                              
                                                wordCount++;
                                            }
                                            gc.boundrule(false, "");
                                            gc.boundgr(false);

                                            // setup recognizer
                                            SpeechRecognitionEngine rec = new SpeechRecognitionEngine();
                                            rec.LoadGrammar(gc.getGrammar());

                                            // recognizing
                                            for (int iSample = 1; iSample <= MSample; iSample++)
                                            {
                                                rec.SetInputToWaveFile(data.getAudioName(iTestSpeaker,iWordType,iSample));
                                                RecognitionResult result;
                                                try
                                                {
                                                    result = rec.Recognize();
                                                }
                                                catch (Exception e)
                                                {
                                                    System.Diagnostics.Debug.WriteLine(e.Message);
                                                    System.Diagnostics.Debug.WriteLine(data.getAudioName(iTestSpeaker, iWordType, iSample));
                                                    continue;
                                                }
                                                
                                                if (result == null)
                                                {
                                                    ConfusionMatrix[NTrainingSpeakers - 1, iTestSpeaker, MSample * (iWordType - 1) + iSample - 1, numberOfAlternates * MWordTypes].num++;
                                                }
                                                else
                                                {
                                                    string[] word = result.Alternates[0].Words[0].Text.Split('_');
                                                    ConfusionMatrix[NTrainingSpeakers - 1, iTestSpeaker, MSample * (iWordType - 1) + iSample - 1, numberOfAlternates * (Array.IndexOf(data.listOfWords.ToArray(), word[0]) - 1) + int.Parse(word[1])].num++;
                                                   
                                                    
                                                }
                                                
                                                if (result != null && result.Alternates[0].Words[0].Text.Split('_')[0].Equals(data.listOfWords[iWordType]))
                                                {
                                                    SampleAccuracy[NTrainingSpeakers - 1, NTrainingSamplesPerSpeaker - 1, Array.IndexOf(V, vocabSize), iTestSpeaker, iWordType - 1, iSample - 1].num++;
                                                    //System.Diagnostics.Debug.WriteLine(NTrainingSpeakers + "_" + NTrainingSamplesPerSpeaker + "_" + Array.IndexOf(V, vocabSize) + "_" + iTestSpeaker + "_" + iWordType + "_" + iSample);
                                                }
                                                SampleAccuracy[NTrainingSpeakers - 1, NTrainingSamplesPerSpeaker - 1, Array.IndexOf(V, vocabSize), iTestSpeaker, iWordType - 1, iSample - 1].den++;
                                                
                                            }

                                            gc.Destroy();
                                        }
                                    }
                                }
                            }


                            // end testing
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Fills in the output data from data in SampeAccuracy
        /// </summary>
        public void createOutput()
        {
           // fill in the output from SampleAccuracy
            
            for (int NTrainingSpeakersIndex = 0; NTrainingSpeakersIndex < MSpeakers - 1; NTrainingSpeakersIndex++)
            {
                for (int NTrainingSamplesPerSpeakerIndex = 0; NTrainingSamplesPerSpeakerIndex < MSample; NTrainingSamplesPerSpeakerIndex++)
                {
                    for (int vocabSizeIndex = 0; vocabSizeIndex < V.Length; vocabSizeIndex++)
                    {
                        Fraction sumOverallAccuracy = new Fraction();
                        for (int iWordTypeIndex = 0; iWordTypeIndex < MWordTypes; iWordTypeIndex++)
                        {
                            Fraction sumOverallWordTypeAccuracy = new Fraction();
                            for (int iTestSpeakerIndex = 0; iTestSpeakerIndex < MSpeakers; iTestSpeakerIndex++)
                            {
                                Fraction sumWordTypeAccuracy = new Fraction();
                                for (int iSamplesIndex = 0; iSamplesIndex < MSample; iSamplesIndex++)
                                {
                                    sumWordTypeAccuracy = sumWordTypeAccuracy + SampleAccuracy[NTrainingSpeakersIndex, NTrainingSamplesPerSpeakerIndex, vocabSizeIndex, iTestSpeakerIndex, iWordTypeIndex, iSamplesIndex];
                                }
                                WordTypeAccuracy[NTrainingSpeakersIndex, NTrainingSamplesPerSpeakerIndex, vocabSizeIndex, iTestSpeakerIndex, iWordTypeIndex] = sumWordTypeAccuracy;
                                sumOverallWordTypeAccuracy = sumOverallWordTypeAccuracy + sumWordTypeAccuracy;
                                
                            }
                            
                            OverallWordTypeAccuracy[NTrainingSpeakersIndex, NTrainingSamplesPerSpeakerIndex, vocabSizeIndex, iWordTypeIndex] = sumOverallWordTypeAccuracy;
                            sumOverallAccuracy = sumOverallAccuracy + sumOverallWordTypeAccuracy;
                            
                        }
                        OverallAccuracy[NTrainingSpeakersIndex, NTrainingSamplesPerSpeakerIndex, vocabSizeIndex] = sumOverallAccuracy;
                    }
                }
            }

            // fill in SampleDeviation
            for (int NTrainingSpeakersIndex = 0; NTrainingSpeakersIndex < MSpeakers - 1; NTrainingSpeakersIndex++)
            {
                for (int NTrainingSamplesPerSpeakerIndex = 0; NTrainingSamplesPerSpeakerIndex < MSample; NTrainingSamplesPerSpeakerIndex++)
                {
                    for(int iTestSpeakerIndex = 0; iTestSpeakerIndex < MSpeakers; iTestSpeakerIndex++)                    
                    {
                        for (int iWordTypeIndex = 0; iWordTypeIndex < MWordTypes; iWordTypeIndex++)
                        {
                            for(int iSampleIndex = 0; iSampleIndex < MSample; iSampleIndex++)
                            {
                                // calculate average
                                Fraction average = new Fraction();
                                Fraction current = new Fraction();
                                // for every sample except the current one being considered
                                for (int iOtherTrainingSamplesPerSpeakerIndex = 0; iOtherTrainingSamplesPerSpeakerIndex < MSample; iOtherTrainingSamplesPerSpeakerIndex++)
                                {
                                    for (int vocabSizeIndex = 0; vocabSizeIndex < V.Length; vocabSizeIndex++)
                                    {
                                        if (iOtherTrainingSamplesPerSpeakerIndex == iSampleIndex)
                                        {
                                            current = current + SampleAccuracy[NTrainingSpeakersIndex, NTrainingSamplesPerSpeakerIndex, vocabSizeIndex, iTestSpeakerIndex, iWordTypeIndex, iSampleIndex];
                                        }
                                        else
                                        {
                                            average = average + SampleAccuracy[NTrainingSpeakersIndex, NTrainingSamplesPerSpeakerIndex, vocabSizeIndex, iTestSpeakerIndex, iWordTypeIndex, iOtherTrainingSamplesPerSpeakerIndex];
                                        }
                                    }
                                }
                                
                                // end average                                
                                if (average.toDecimal() - current.toDecimal() > threshold)
                                {
                                    SampleDeviation[NTrainingSpeakersIndex, NTrainingSamplesPerSpeakerIndex, iTestSpeakerIndex, iWordTypeIndex].Add(iSampleIndex);
                                }
                            }


                           
                        }
                    }
                }
            }

            // fill in ErrorRates
            for (int NTrainingSpeakersIndex = 0; NTrainingSpeakersIndex < MSpeakers - 1; NTrainingSpeakersIndex++)
            {

                for (int iTestSpeakerIndex = 0; iTestSpeakerIndex < MSpeakers; iTestSpeakerIndex++)
                {
                    
                    for (int iCol = 0; iCol < numberOfAlternates * MWordTypes; iCol++)
                    {
                        Fraction eagerness = new Fraction();
                        Fraction shyness = new Fraction();
                        int wordTypeIndex = iCol / numberOfAlternates;
                        for (int iRow = 0; iRow < MSample * MWordTypes; iRow++)
                        {
                            if ((iRow / MSample) == wordTypeIndex)
                            {
                                shyness = shyness + ConfusionMatrix[NTrainingSpeakersIndex, iTestSpeakerIndex, iRow, iCol];
                            }
                            else
                            {
                                eagerness = eagerness + ConfusionMatrix[NTrainingSpeakersIndex, iTestSpeakerIndex, iRow, iCol];
                            }
                        }
                        ErrorRates[NTrainingSpeakersIndex, iTestSpeakerIndex, 0, iCol] = eagerness;
                        ErrorRates[NTrainingSpeakersIndex, iTestSpeakerIndex, 1, iCol] = shyness;
                    }
                    
                }

                
            }
        }

        /********************************************************************************************/
        /*
        /* Runs test methods
        /* 
        /***********************************************************************************************/

        /// <summary>
        /// Runs everything
        /// </summary>
        /// <param name="outputName">name of the output .csv files</param>
        public void runTest(string outputName, List<bool> outputChoice)
        {
            testingAlgorithm();
            createOutput();
            prettyPrinting(outputName, outputChoice);

        }

        /// <summary>
        /// Runs everything
        /// </summary>
        /// <param name="outputName">name of the output .csv files</param>
        public void runTestDiscriminative(string outputName, List<bool> outputChoice)
        {
            testingAlgorithmDiscriminative();
            createOutput();
            prettyPrinting(outputName, outputChoice);

        }

        /***********************************************************************/
        /*
        /*Printing output methods 
        /*
        /************************************************************************/

        /// <summary>
        /// Produces the .csv files
        /// </summary>
        /// <param name="outputName">output file name</param>
        public void prettyPrinting(string outputName, List<bool> outputChoice)
        {
            
            for (int NTrainingSpeakersIndex = 0; NTrainingSpeakersIndex < MSpeakers - 1; NTrainingSpeakersIndex++)
            {
                for(int iTestSpeakerIndex = 0; iTestSpeakerIndex < MSpeakers; iTestSpeakerIndex++)
                {
                    string output = "";
                    if (outputChoice[0])
                        // data output
                        output += dataOutput(NTrainingSpeakersIndex, iTestSpeakerIndex) + "\n";
                    
                    if (outputChoice[1])
                        // sample deviation output
                        output += sampleDeviationOutput(NTrainingSpeakersIndex, iTestSpeakerIndex) + "\n";

                    if (outputChoice[2])
                        // confusion matrix output
                        output += confusionMatrixOutput(NTrainingSpeakersIndex, iTestSpeakerIndex) + "\n";

                    if (outputChoice[3])
                        // error rates output
                        output += errorRatesOutput(NTrainingSpeakersIndex, iTestSpeakerIndex) + "\n";
                    
                    // write to file
                    File.AppendAllText(grammarDirectory + "\\" +
                                        data.language + "_" +
                                        (NTrainingSpeakersIndex + 1).ToString() + "_" + 
                                        5 + "_" + 
                                        (iTestSpeakerIndex + 1).ToString() + "_" + 
                                        outputName + "_" +
                                        "output.csv", output);
                }
            }
            
            
        }

        private string errorRatesOutput(int NTrainingSpeakersIndex, int iTestSpeakerIndex)
        {
            string output = "Error Rates\n";
            output += "Eagerness,";

            for (int i = 0; i < numberOfAlternates * MWordTypes; i++)
            {
                Fraction data = ErrorRates[NTrainingSpeakersIndex, iTestSpeakerIndex, 0, i];
                if (data.den == 0) continue;
                output += data + ",";
            }

            output += "\n";


            output += "Shyness,";

            for (int i = 0; i < numberOfAlternates * MWordTypes; i++)
            {
                Fraction data = ErrorRates[NTrainingSpeakersIndex, iTestSpeakerIndex, 1, i];
                if (data.den == 0) continue;
                output += data + ",";
            }

            output += "\n";
            return output;
        }
        
        private string confusionMatrixOutput(int NTrainingSpeakersIndex, int iTestSpeakerIndex)
        {
            string output = "Confusion Matrix\n,";
            // reading grammar file to create header
            List<List<int>> speakerList = new List<List<int>>();
            List<List<int>> allSpeakerList = Tester.choose(MSpeakers, NTrainingSpeakersIndex + 1);

            // filtering true speaker list
            foreach (List<int> tempSpeakerList in allSpeakerList)
            {
                if (tempSpeakerList.Count == NTrainingSpeakersIndex + 1 && !tempSpeakerList.Contains(iTestSpeakerIndex))
                {
                    speakerList.Add(tempSpeakerList);
                    //break;
                }
            }
            List<int> sampleList = new List<int>();
            for (int i = 0; i < MSample; i++)
            {
                sampleList.Add(i + 1);
            }

            // merging all the keys
            // wordType : Number of pronunciations available
            Dictionary<string,int> allPronun = new Dictionary<string,int>();
            Dictionary<string, List<string>> pronunciations;
            foreach (List<int> realSpeakerList in speakerList)
            {
                pronunciations = input.outputDiscriminativeEliminative(realSpeakerList, sampleList, (iTestSpeakerIndex + 1));
                foreach (string wordType in pronunciations.Keys)
                {
                    int newNumberOfAlt = pronunciations[wordType].Count;
                    if (allPronun.ContainsKey(wordType))
                    {
                        int numberOfAlt = allPronun[wordType];
                        // get the max number of pronunciations
                        if (numberOfAlt < newNumberOfAlt)
                        {
                            allPronun.Remove(wordType);
                            allPronun.Add(wordType, newNumberOfAlt);
                        }

                    }
                    else
                        allPronun.Add(wordType,newNumberOfAlt);
                }
            }

            // creating header
            foreach (string wordType in allPronun.Keys)
            {
                int count = allPronun[wordType];
                for (int i = 0; i < count; i++)
                    output += wordType + "_" + i+",";
            }
            /*for (int iWordTypeIndex = 0; iWordTypeIndex < MWordTypes; iWordTypeIndex++)
            {
                for (int numberOfAlternatesIndex = 0; numberOfAlternatesIndex < numberOfAlternates; numberOfAlternatesIndex++)
                {
                    Fraction fraction = ConfusionMatrix[NTrainingSpeakersIndex, iTestSpeakerIndex, MSample * iWordTypeIndex + 0, numberOfAlternates * iWordTypeIndex + numberOfAlternatesIndex];
                    if (fraction.den == 0)
                        continue;
                    output += data.listOfWords[iWordTypeIndex + 1] + "_" + numberOfAlternatesIndex + ",";
                }
            }*/
            output += "NULL\n";
            
            for (int iWordTypeIndex = 0; iWordTypeIndex < MWordTypes; iWordTypeIndex++)
            {
                System.Diagnostics.Debug.WriteLine("wordType output: " + iWordTypeIndex);
                for (int sampleIndex = 0; sampleIndex < MSample; sampleIndex++)
                {
                    output += data.listOfWords[iWordTypeIndex + 1] + "_" + "sample" + sampleIndex + ",";
                    for (int iConfusedWordTypeIndex = 0; iConfusedWordTypeIndex < MWordTypes; iConfusedWordTypeIndex++)
                    {
                        
                        for (int numberOfAlternatesIndex = 0; numberOfAlternatesIndex < numberOfAlternates; numberOfAlternatesIndex++)
                        {
                            Fraction fraction = ConfusionMatrix[NTrainingSpeakersIndex, iTestSpeakerIndex, MSample * iWordTypeIndex + sampleIndex, numberOfAlternates * iConfusedWordTypeIndex + numberOfAlternatesIndex];
                            if (fraction.den == 0)
                            {
                                continue;
                            }
                            else if (iConfusedWordTypeIndex == iWordTypeIndex && fraction.num == 0)
                                output += "*,";

                            else if (fraction.num != 0)
                                //output += ConfusionMatrix[NTrainingSpeakersIndex, iTestSpeakerIndex, MSample * iWordTypeIndex + sampleIndex, numberOfAlternates * iConfusedWordTypeIndex + numberOfAlternatesIndex] + ",";
                                output += (int)(fraction.toDecimal() * 100) + "  ||  " + fraction + ",";
                            else
                            {
                                output += ",";
       
                            }
                        }
                    }
                    output += ConfusionMatrix[NTrainingSpeakersIndex, iTestSpeakerIndex, MSample * iWordTypeIndex + sampleIndex, numberOfAlternates * MWordTypes] + ",";
                    output += "\n";
                }
            }
            return output;
        }

        private string sampleDeviationOutput(int NTrainingSpeakersIndex, int iTestSpeakerIndex)
        {
            string output = "Deviations\n";
            // data for deviation
            for (int iWordTypeIndex = 0; iWordTypeIndex < MWordTypes; iWordTypeIndex++)
            {
                if (SampleDeviation[NTrainingSpeakersIndex, 4, iTestSpeakerIndex, iWordTypeIndex].Count != 0)
                {
                    output += data.listOfWords[iWordTypeIndex + 1] + ",";
                    foreach (int sample in SampleDeviation[NTrainingSpeakersIndex, 4, iTestSpeakerIndex, iWordTypeIndex])
                    {
                        output += sample + ",";
                    }
                    output += "\n";
                }
            }

            return output;
        }

        private string dataOutput(int NTrainingSpeakersIndex, int iTestSpeakerIndex)
        {
            string output = ",";
            // header
            for (int vocabSizeIndex = 0; vocabSizeIndex < V.Length; vocabSizeIndex++)
            {
                output += V.ElementAt(vocabSizeIndex) + ",";
            }
            output += "Overall\n";
            // actual data
            List<string> problemWordTypes = new List<string>();
            for (int iWordTypeIndex = 0; iWordTypeIndex < MWordTypes; iWordTypeIndex++)
            {
                output += data.listOfWords[iWordTypeIndex + 1] + ",";
                Fraction sum = new Fraction();
                for (int vocabSizeIndex = 0; vocabSizeIndex < V.Length; vocabSizeIndex++)
                {
                    sum = sum + WordTypeAccuracy[NTrainingSpeakersIndex, 4, vocabSizeIndex, iTestSpeakerIndex, iWordTypeIndex];
                    output += WordTypeAccuracy[NTrainingSpeakersIndex, 4, vocabSizeIndex, iTestSpeakerIndex, iWordTypeIndex] + ",";
                    if (vocabSizeIndex == V.Length - 1 && WordTypeAccuracy[NTrainingSpeakersIndex, 4, vocabSizeIndex, iTestSpeakerIndex, iWordTypeIndex].toDecimal() < 0.8)
                        problemWordTypes.Add(data.listOfWords[iWordTypeIndex + 1]);
                }
                output += sum;
                output += "\n";
            }

            // overall summary
            output += "Overall,";
           
            for (int vocabSizeIndex = 0; vocabSizeIndex < V.Length; vocabSizeIndex++)
            {
                Fraction sum = new Fraction();
                for (int iWordTypeIndex = 0; iWordTypeIndex < MWordTypes; iWordTypeIndex++)
                {
                    sum = sum + WordTypeAccuracy[NTrainingSpeakersIndex, 4, vocabSizeIndex, iTestSpeakerIndex, iWordTypeIndex];
                    
                }
                output += sum + ",";
            }
            output += "\n";

            // problem word types
            output += "Problem word types,";
            foreach (string wordType in problemWordTypes)
            {
                output += wordType + ",";
            }
            output += "\n";
            return output;
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
            trainingSet[0].AddRange(choose(MSpeakers, number_of_speakers));
            trainingSet[1].AddRange(choose(MSample, number_of_samples));
           
        }

        /// <summary>
        /// Given a list of speakers and the samples per speakers to train on, training is carried out to produce a List of pronunciations
        /// </summary>
        /// <param name="speakerList">list of speakers for training</param>
        /// <param name="sampleList">list of samples for training</param>
        /// <returns>List of pronunciations</returns>
        private Dictionary<string, List<string>> trainAllWords(List<int> speakerList, List<int> sampleList)
        {
            string speaker = "speaker-";
            for (int i = 0; i < speakerList.Count; i++)
            {
                speaker += speakerList[i].ToString()+"_";
            }
            string sample = "sample-";


            List<int> trueSampleList = new List<int>();
            trueSampleList.AddRange(sampleList);
            for (int i = 0; i < trueSampleList.Count; i++)
            {
                trueSampleList[i]++;
                sample += trueSampleList[i].ToString() + "_";
            }
            System.Diagnostics.Debug.WriteLine("training " + speaker +  sample);


            // words to be tested
            List<int> wordsToBeTested = new List<int>();
             for (int i = 1; i <= MWordTypes; i++)
                 wordsToBeTested.Add(i);

             // set up data
             Data tempData = new Data(data.audioDirectory, data.audioFileName, data.numberOfSamplesPerWord, speakerList, wordsToBeTested, trueSampleList,
                 data.wordListPath);

             // setup grammar
             GrammarCreator gc = new GrammarCreator(grammarDirectory, data.language + speaker + sample, "allcombinations");

             // setup training
             TrainingAlgorithm ta = new TrainingAlgorithm(gc, tempData, numberOfAlternates, 15);

             return ta.LearnAllWords();

        }

        /************************************************************************************************************/
        /*
        /* Helpful methods
        /*
        /************************************************************************************************************/
        /// <summary>
        /// Creates the different combinations possible from M choose N
        /// </summary>
        /// <param name="M">M</param>
        /// <param name="N">N</param>
        /// <returns>A List of List of int where the outer List represents the sets of combinations and the inner List contains the items itself</returns>
        public static List<List<int>> choose(int M, int N)
        {
            return chooseRecursive(N, M, null, 0);
        }

        /// <summary>
        /// Recursive form of choose, actually performing the calculation
        /// </summary>
        /// <param name="size">number chosen</param>
        /// <param name="max_size">number to choose from</param>
        /// <param name="prev_result">result from previous iteration</param>
        /// <param name="prev_size">previous number chosen</param>
        /// <returns></returns>
        private static List<List<int>> chooseRecursive(int size, int max_size, List<List<int>> prev_result, int prev_size)
        {
            List<List<int>> currResult;
            // for the start case where everything is empty
            if (prev_size == 0)
            {
                currResult = new List<List<int>>();
                for (int i = 0; i < max_size; i++)
                {
                    List<int> newRow = new List<int>();
                    newRow.Add(i);
                    currResult.Add(newRow);
                }
                return chooseRecursive(size, max_size, currResult, prev_size + 1);
            }

            // recursive method
            if (prev_size == size) return prev_result; // base case
            currResult = new List<List<int>>();
            for (int i = 0; i < prev_result.Count - 1; i++)
            {
                List<List<int>> temp = new List<List<int>>();
                for (int j = prev_result[i][prev_size-1] + 1; j < max_size; j++)
                {
                    
                    List<int> newRow = new List<int>();
                    newRow.AddRange(prev_result[i]);
                    
                    newRow.Add(j);
                    temp.Add(newRow);
                   
                }
                currResult.AddRange(temp);
            }
            return chooseRecursive(size, max_size, currResult, prev_size + 1);
        }

    }
}
