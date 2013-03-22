using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.IO;
using Microsoft.Speech.Recognition;

namespace Salaam
{
    
    public class TrainingAlgorithm
    {
        // data required
        static SpeechRecognitionEngine rec;      // speech recognizer used to prune results
        private GrammarCreator gc;              // used to create the final .grxml file
        private Data data;                      // data
        private int resultSize;            /* The number of pronunciations we generate for each word. */
        private string directory;               // directory where all is done

        // fields for iterative improvement
        private GrammarCreator all3phonemes;
        private GrammarCreator all2phonemes;
        private GrammarCreator mixedPhonemes;
        private GrammarCreator mixedPhonemes1;
        private Hashtable [] wordToConfidenceHash;      /* Hashtables for storing confidences of various phonemes obtained from training samples. */
        private SortedDictionary<double, List<string>>[] confidenceToWordMapArray;
        private List<string> diversityList;     /* Keep track of the top items from the n-best list for looking next time. */
        private List<string> currentWordResults; /* Resulting pronunciations for the current word. */
        private int numberOfMinRecognizedWord = 15;       /* The minimum number of slots in the hashtable for recognized words. */
        List<string> diversityListPrev;
        List<string> diversityListPrev1;
        // used for termination
        private string bestAlternative;        /* The all the phonemes obtained from last pass, for us to compare results and discover alternatives. */
        private string bestEndResult;          /* The phonemes we are certain of currently. */
        private string breakoutAlternative;    /* Keeps track of the last best learning result once we are not getting any more phonemes. */
       
        private int numberOfPasses;               /* Counter to how many passes has the progressively improving hints gone through for the current word. */
        private int tries;                     // how many tries it takes to create the first recognition
        bool wordIsComplete;                   // is the word complete?
        int methodToGenerateInitialGrammar;          // different methods to generate initial grammar

        //output files
        string outputFile;                    // file name of the training recognition process

        public class DescendingComparer<T> : IComparer<T> where T : IComparable<T>
        {
            public int Compare(T x, T y)
            {
                return y.CompareTo(x);
            }
        }


        public TrainingAlgorithm(GrammarCreator grammar_creator, Data d, int result_size, int max_alternatives)
        {
            rec = new SpeechRecognitionEngine();
            rec.MaxAlternates = max_alternatives;
            gc = grammar_creator;
            data = d;
            resultSize = result_size;
            directory = gc.grammarDir;
            
            outputFile = "TrainingRecognition_";
            foreach (int i in data.speakersToBeTested)
            {

                outputFile += i + "_";
            }
            
            all3phonemes = new GrammarCreator(directory, "3phonemes", "allcombinations");
            all3phonemes.buildGrammarFileWithItems(all3phonemes.allcombinations("", 0, 3));

            all2phonemes = new GrammarCreator(directory, "2phonemes", "allcombinations");
            all2phonemes.buildGrammarFileWithItems(all2phonemes.allcombinations("", 0, 2));

            mixedPhonemes = new GrammarCreator(directory, "mixedphonemes", "allcombinations");
            mixedPhonemes.boundgr(true);
            mixedPhonemes.boundrule(true, mixedPhonemes.grammarRuleName);
            mixedPhonemes.buildItemWithRepeats(all2phonemes.grammarPath + "#" + all2phonemes.grammarRuleName, 0, 10);
            mixedPhonemes.buildItem(all3phonemes.grammarPath + "#" + all2phonemes.grammarRuleName);
            mixedPhonemes.boundrule(false, "");
            mixedPhonemes.boundgr(false);

            mixedPhonemes1 = new GrammarCreator(directory, "mixedphonemes1", "allcombinations");
            mixedPhonemes1.boundgr(true);
            mixedPhonemes1.boundrule(true, mixedPhonemes1.grammarRuleName);
            mixedPhonemes1.LogGrammar("<item>\n");
            mixedPhonemes1.buildRuleRef("#listGr");
            mixedPhonemes1.buildItemWithRepeats(all2phonemes.grammarPath + "#" + all2phonemes.grammarRuleName, 0, 10);
            mixedPhonemes1.LogGrammar("</item>\n");
            mixedPhonemes1.boundrule(false, "");

            mixedPhonemes1.buildRuleWithItems("listGr", mixedPhonemes1.allcombinations("", 0, 2));

            mixedPhonemes1.boundgr(false);
        }

        ~TrainingAlgorithm()
        {
            //all3phonemes.Destroy();
            //all2phonemes.Destroy();
            //mixedPhonemes.Destroy();
        }

        private void initWordToConfidenceHash()
        {
            wordToConfidenceHash = new Hashtable [numberOfMinRecognizedWord];
            for (int i = 0; i < numberOfMinRecognizedWord; i++)
            {
                wordToConfidenceHash[i] = new Hashtable();
            }
        }
        /// <summary>
        /// Learns all the words as specified in data
        /// Effects: Create a .grxml file for all the words requested
        /// </summary>
        /// <returns>List of final pronunciations</returns>
        public Dictionary<string, List<string>> LearnAllWords()
        {
            List<string> pronunciations = new List<string>();
            List<string> grammarItems = new List<string>();
            Dictionary<string, List<string>> pronuciation_mapping = new Dictionary<string, List<string>>();
            gc.boundgr(true);
            gc.boundrule(true, gc.grammarRuleName);

            string outputHeader = "Word,Speaker,Sample,Iteration\n";
            File.AppendAllText(directory + "\\" + outputFile + ".csv", outputHeader);

            for (int i = 0; i < data.numberOfWords; i++)
            {
                pronunciations.Clear();
                if (data.wordsToBeTested.Contains(i+1))
                {
                    System.Diagnostics.Debug.WriteLine("Word: " + (i + 1).ToString());
                    pronunciations.AddRange(LearnWord(i + 1));
                    pronuciation_mapping[data.listOfWords[i + 1]] = new List<string>();
                    foreach (string pronun in pronunciations)
                    {

                        pronuciation_mapping[data.listOfWords[i + 1]].Add(pronun.Replace('_', ' '));
                    }
                    List<string> items = gc.tagAsItems(pronunciations);

                    for (int j = 0; j < items.Count; j++)
                    {
                        System.Diagnostics.Debug.WriteLine("pro: " + items[j]);
                        // remove pronunciations with only 1 phoneme
                        if (pronunciations[j].Split('_').Length == 1) continue;
                        grammarItems.Add(items[j].Replace(pronunciations[j], data.listOfWords[i + 1] ));
                        gc.LogGrammar(items[j].Replace(pronunciations[j], data.listOfWords[i + 1] + "_" + j));
                    }
                    
                }
            }

            gc.boundrule(false, "");
            gc.boundgr(false);

            return pronuciation_mapping;
            //return grammarItems;
        }

        /// <summary>
        /// Learns a single word
        /// </summary>
        /// <param name="word_number">index of the word to be learnt</param>
        /// <returns>
        /// List of generated pronunciations, each of the form "phoneme1_phoneme2_..._phonemeN"
        ///</returns>
        public List<string> LearnWord(int word_number)
        {
            diversityList = new List<string>();
            currentWordResults = new List<string>();
            tries = 0;
            bestAlternative = "";
            bestEndResult = "";
            breakoutAlternative = "";
            numberOfPasses = 0;
            wordIsComplete = false;
            methodToGenerateInitialGrammar = 0;

            initWordToConfidenceHash();

            string[] bestMSSWords = null;

            List<string> finalResult = new List<string>();

            RecognitionResult result = null;

            diversityListPrev = new List<string>();
            diversityListPrev1 = null;

            // prep for output
            int iteration = 0;
           

            while (!wordIsComplete)
            {
                //diversityListPrev = new List<string>();   
                
                // testing
                for (int i = 0; i < diversityList.Count; i++)
                {
                    System.Diagnostics.Debug.WriteLine(numberOfPasses + ": " + diversityList[i]);
                    
                }
               
                if (numberOfPasses == 0)
                {
                    if (!loadInitialGrammar(methodToGenerateInitialGrammar, word_number))
                    {
                        finalResult.Add("Unable_To_Find_Pronunciation");
                        return finalResult;
                    }
                }
                else
                {
                    loadGrammar(numberOfPasses, word_number);
                }
                foreach (string i in diversityList)
                {
                    System.Diagnostics.Debug.WriteLine("diver: " + i);
                }
                
                //diversityListPrev = new List<string>(diversityList);
                diversityList.Clear();
                //foreach (string i in diversityListPrev)
                //{
                //    System.Diagnostics.Debug.WriteLine("diverPrev: " + i);
                //}
                                
                for (int i = 0; i < data.numberOfSpeakers; i++)
                {

                    if (data.speakersToBeTested.Contains(i))
                    {
                        for (int j = 1; j <= data.numberOfSamplesPerWord; j++)
                        {
                            if (data.samplesToBeTested.Contains(j))
                            {
                                
                                rec.SetInputToWaveFile(data.getAudioName(i, word_number, j));

                                // recognise
                                try
                                {
                                    result = rec.Recognize();
                                }
                                catch (Exception e)
                                {
                                    System.Diagnostics.Debug.WriteLine(" LearnWord: "+e.Message);
                                    System.Diagnostics.Debug.WriteLine(data.getAudioName(i, word_number, j));
                                    continue;
                                }
                                if (result == null)
                                {
                                    string outputPerAudioFile = "";
                                    outputPerAudioFile += word_number + ",\t" + i + ",\t" + j + ",\t"+ iteration + "\n";
                                    System.Diagnostics.Debug.WriteLine(outputPerAudioFile);
                                    File.AppendAllText(directory + "\\" + outputFile + ".csv", outputPerAudioFile);
                                    continue;
                                }
                                else
                                {

                                    parseRecognitionResult(result);
                                }
                            }
                        }
                    }
                }


                if ((result == null && diversityList.Count == 0 && diversityListPrev.Count != 0 && bestEndResult.Equals(""))||
                    (result == null && diversityList.Count == 0 && numberOfPasses != 0))
                {
                    
                    if (diversityListPrev1 != null && UnorderedEqual(diversityListPrev1, diversityListPrev))
                    {
                        List<string> phonemes = new List<string>();
                        foreach (string pronun in diversityListPrev)
                        {
                            GrammarCreator grammarMaker = new GrammarCreator("", "", "");
                            phonemes.AddRange(grammarMaker.allcombinations(pronun, 0, pronun.Split('_').Length + 1));
                            System.Diagnostics.Debug.WriteLine("adding: " + pronun);
                        }
                        diversityList = new List<string>(phonemes);
                    }
                    else
                        diversityList = new List<string>(diversityListPrev);
                    diversityListPrev1 = new List<string>(diversityListPrev);
                    //if (bestEndResult.Equals(""))
                        //diversityList.AddRange(diversityListPrev);
                    tries = tries + 1;
                    //numberOfPasses++;
                }
                else if (result == null && diversityList.Count == 0)
                {
                    
                    methodToGenerateInitialGrammar++;
                    System.Diagnostics.Debug.WriteLine("failed");
                    numberOfPasses = 0;
                }
                else
                {
                    bestMSSWords = outputPass();
                    methodToGenerateInitialGrammar = 0;
                    wordToConfidenceHash = null;
                    initWordToConfidenceHash();
                    /*if (diversityList.Count == 0)
                    {
                        System.Diagnostics.Debug.WriteLine("diversity list empty");
                        diversityList = new List<string>(diversityListPrev);
                    }
                    else
                    {
                        diversityListPrev = new List<string>(diversityList);
                        foreach (string i in diversityListPrev)
                        {
                            System.Diagnostics.Debug.WriteLine("diverPrev: " + i);
                        }
                    }*/
                    
                    //diversityListPrev.Clear();
                    System.Diagnostics.Debug.WriteLine("clear");
                    
                    numberOfPasses++;
                }

                checkForTermination(bestMSSWords);

                iteration++;
            }
           
            finalResult.AddRange(currentWordResults);
            return finalResult;
        }

        public bool loadInitialGrammar(int method_number, int word_number)
        {
            
            switch (method_number)
            {
                case 0:
                    {
                        rec.UnloadAllGrammars();
                        rec.LoadGrammar(all3phonemes.getGrammar());

                        break;
                    }
                case 1:
                    {
                        rec.UnloadAllGrammars();
                        rec.LoadGrammar(mixedPhonemes.getGrammar());
                        break;
                    }
                case 2:
                    {
                        rec.UnloadAllGrammars();
                        rec.LoadGrammar(mixedPhonemes1.getGrammar());
                        break;
                    }
                case 3:
                    {
                        for (int i = 0; i < GrammarCreator.enusphoneme.Length; i++)
                        {
                            diversityList.Add(GrammarCreator.enusphoneme[i]);
                        }
                        loadGrammar(1, word_number);
                        break;
                    }
                
                default:
                    {
                        System.Diagnostics.Debug.WriteLine("Unable to find pronunciation");
                        wordIsComplete = true;
                        return false;
                    }
            }
            return true;

        }

        public void loadGrammar(int pass, int word_number)
        {
            GrammarCreator finalGrammar = new GrammarCreator(directory, pass.ToString(), "divAndCon");
            List<string> resultFromDivideAndConquer = new List<string>();
            List<string> full_phonemes = new List<string>();
            for (int i = 0; i < diversityList.Count; i++)
            {
                GrammarCreator grammarMaker = new GrammarCreator(directory, pass + "_" + i.ToString(), "temp");
                grammarMaker.boundgr(true);
                grammarMaker.boundrule(true, grammarMaker.grammarRuleName);
                grammarMaker.boundItem(true);
                grammarMaker.buildRuleRef("#listGr");
                grammarMaker.buildItemWithRepeats(all2phonemes.grammarPath + "#" + all2phonemes.grammarRuleName, 0, 15);
                grammarMaker.boundItem(false);
                grammarMaker.boundrule(false, "");
                System.Diagnostics.Debug.WriteLine("D:" + diversityList[i]);
                List<string> phonemes = grammarMaker.allcombinations(diversityList[i], 0, diversityList[i].Split('_').Length+1);
                full_phonemes.AddRange(phonemes);
                phonemes.Add(diversityList[i]);
                grammarMaker.buildRuleWithItems("listGr", phonemes);

                grammarMaker.boundgr(false);

                
                resultFromDivideAndConquer.AddRange(filter(grammarMaker, word_number));

                grammarMaker.Destroy();

            }
            if (resultFromDivideAndConquer.Count < 2)
            {
              //  System.Diagnostics.Debug.WriteLine("No Filter");
                foreach (string phoneme in full_phonemes)
                {
                    resultFromDivideAndConquer.Add(phoneme);
                }
                tries = tries+1;
            }
            diversityListPrev = new List<string>(resultFromDivideAndConquer);
            //diversityListPrev.AddRange(full_phonemes);
            //for (int i = 0; i < diversityListPrev.Count; i++)
            //{
             // System.Diagnostics.Debug.WriteLine("divlistPrev: " + diversityListPrev[i]);
            //}
            rec.UnloadAllGrammars();
            finalGrammar.boundgr(true);
            finalGrammar.boundrule(true, finalGrammar.grammarRuleName);
            finalGrammar.boundItem(true);
            finalGrammar.buildRuleRef("#listGr");
            finalGrammar.buildItemWithRepeats(all2phonemes.grammarPath + "#" + all2phonemes.grammarRuleName, 0, 15);
            finalGrammar.boundItem(false);
            finalGrammar.boundrule(false, "");

            finalGrammar.buildRuleWithItems("listGr", resultFromDivideAndConquer);

            finalGrammar.boundgr(false);
            rec.UnloadAllGrammars();
            rec.LoadGrammar(finalGrammar.getGrammar());
            finalGrammar.Destroy();
            
        }

        public List<string> filter(GrammarCreator gc, int word_number)
        {
            SpeechRecognitionEngine filterRec = new SpeechRecognitionEngine();
            RecognitionResult result = null;
            List<string> pronunciations = new List<string>();
            
            filterRec.UnloadAllGrammars();
            filterRec.LoadGrammar(gc.getGrammar());
            filterRec.MaxAlternates = rec.MaxAlternates;
            for (int i = 0; i < data.numberOfSpeakers; i++)
            {
                if (data.speakersToBeTested.Contains(i))
                {
                    for (int j = 1; j <= data.numberOfSamplesPerWord; j++)
                    {
                        if (data.samplesToBeTested.Contains(j))
                        {
                            //System.Diagnostics.Debug.WriteLine(data.getAudioName(i, word_number, j));
                            filterRec.SetInputToWaveFile(data.getAudioName(i, word_number, j));
                            try
                            {
                                result = filterRec.Recognize();
                            }
                            catch (Exception e)
                            {
                                System.Diagnostics.Debug.WriteLine(e.Message);
                                System.Diagnostics.Debug.WriteLine(data.getAudioName(i, word_number, j));
                                continue;
                            }
                        }
                        if (result == null)
                        {
                            continue;
                        }
                        else
                        {
                            foreach (RecognizedPhrase phr in result.Alternates)
                            {
                                if (!pronunciations.Contains(phr.Words[0].Text))
                                {
                                    System.Diagnostics.Debug.WriteLine("found.");
                                    pronunciations.Add(phr.Words[0].Text);
                                }
                            }
                            
                        }
                    }
                }
            }

            return pronunciations;
        }

        public void checkForTermination(string[] bestMSSWords)
        {
            // no results, we do not terminate
            if (bestMSSWords == null) return;

            string[] bestPhonemes = bestMSSWords[0].Split('_');

            /* termination decision
             * if we have only 1 MSS word identified or we have less phonemes then the number of passes
             * then we consider terminating
             */
            //System.Diagnostics.Debug.WriteLine("bestEnd: " + bestEndResult + " bestAlt: " + bestAlternative);
            if ((bestMSSWords.Length == 1) || (bestPhonemes.Length < numberOfPasses))
            {

                // subcases for termination
                if (bestAlternative.Equals(bestEndResult) || breakoutAlternative.Equals(bestEndResult) || (bestPhonemes.Length + 3) < numberOfPasses)
                {
                    wordIsComplete = true;
                    bestEndResult = bestMSSWords[0];
                   
                    SortedDictionary<double, List<string>>.Enumerator enumer = confidenceToWordMapArray[0].GetEnumerator();
                    
                    // pick out best pronunciations up to result_size.
                  
                    while (enumer.MoveNext() && currentWordResults.Count < resultSize)
                    {
                        for (int i = 0; i < enumer.Current.Value.Count && currentWordResults.Count < resultSize  && !currentWordResults.Contains(enumer.Current.Value[i]); i++)
                        {
                            currentWordResults.Add(enumer.Current.Value[i]);
                        }
                    }
                  
                    
                    return;
                }
                /* if we do not terminate, we realize that we almost terminated
                 * => the current bestPhonemes could be a very good result
                 * => store it as the breakout_alternative
                 */
                else
                {
                    breakoutAlternative = bestAlternative;
                }
            }
        }

        public string[] outputPass()
        {
            // initalize variables
            string[] bestMSSWords = new string[numberOfMinRecognizedWord];
            confidenceToWordMapArray = new SortedDictionary<double, List<string>>[numberOfMinRecognizedWord];
            for (int i = 0; i < numberOfMinRecognizedWord; i++)
            {
                confidenceToWordMapArray[i] = new SortedDictionary<double, List<string>>(new DescendingComparer<double>());
            }

            // fill in confidenceToWordMapArray
            for (int i = 0; i < numberOfMinRecognizedWord; i++)
            {
                if (wordToConfidenceHash[i].Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine("wordToConfidenceHash[ " + i +" ] is empty");
                    continue;
                }
                foreach (string phrase in wordToConfidenceHash[i].Keys)
                {
                    double confidence = (double)wordToConfidenceHash[i][phrase];
                    if (!confidenceToWordMapArray[i].ContainsKey(confidence))
                    {
                        confidenceToWordMapArray[i][confidence] = new List<string>();
                    }

                    confidenceToWordMapArray[i][confidence].Add(phrase);
                }
                bestMSSWords[i] = confidenceToWordMapArray[i].First().Value[0];
            }

            string[] bestPhonemes = bestMSSWords[0].Split('_');


            /* For the first pass 
             * record 1st phoneme for bestEndResult
             * record first two phonemes for bestAlternative
             */
            if (numberOfPasses == 0)
            {
                
                bestEndResult = bestPhonemes[0];
                bestAlternative += (bestPhonemes.Length > 1) ? (bestEndResult + "_" + bestPhonemes[1]) : "";
               
            }
            else
            {
                /* For the rest of the passes, record one more phoneme, and keep everything before. */
                string[] previousBestPhonemes = bestAlternative.Split('_');
                bestAlternative = "";
                bestEndResult = "";
               
                int count = 0;


               for (int j = 0; j < bestMSSWords.Length && count <= previousBestPhonemes.Length; j++)
                {
                    if (bestMSSWords[j] == null) break;
                    string[] MSSWordBestPhonemes = bestMSSWords[j].Split('_');
                    for (int k = 0; k < MSSWordBestPhonemes.Length && count <= previousBestPhonemes.Length; k++)
                    {
                        if (bestAlternative.Equals(""))
                        {
                            bestAlternative = MSSWordBestPhonemes[k];
                        }
                        else
                            bestAlternative += "_" + MSSWordBestPhonemes[k];
                        
                        if (count < previousBestPhonemes.Length)
                        {
                            if (bestEndResult.Equals(""))
                            {
                                bestEndResult = MSSWordBestPhonemes[k];
                                }
                            else
                                // best_endresult adds till last_best_arr.Length - 1
                                bestEndResult += "_" + MSSWordBestPhonemes[k];
                        }

                        count++;
                    }

                }
                /*bestAlternative = bestPhonemes[0];
                bestEndResult = bestPhonemes[0];
                for (int i = 1; i <= previousBestPhonemes.Length && i < bestPhonemes.Length; i++)
                {
                    
                    // best_alternative adds till last_best_arr.Length
                    bestAlternative += "_" + bestPhonemes[i];

                    if (i < previousBestPhonemes.Length)
                    {
                        // best_endresult adds till last_best_arr.Length - 1
                        bestEndResult += "_" + bestPhonemes[i];
                    }

                }*/
            }
            //if (!diversityList.Contains(bestEndResult))
               // diversityList.Add(bestEndResult);

            return bestMSSWords;
        }

        public void parseRecognitionResult(RecognitionResult result)
        {
            System.Diagnostics.Debug.WriteLine("in parse");
            foreach (RecognizedPhrase phr in result.Alternates)
            {
                string[] phraseFirstWordPhonemeArray = phr.Words[0].Text.Split('_');
                //int bestEndResultLength = bestEndResult.Equals("") ? 0 : (bestEndResult.Split('_').Length);
                System.Diagnostics.Debug.WriteLine("tries: " + tries);
                int bestEndResultLength = bestEndResult.Equals("") ? tries : (bestEndResult.Split('_').Length);
                string altBestResult = "";

                // get 1 phoneme more then best_endresult from alternatives or less if not possible
                for (int i = 0; (i <= bestEndResultLength && i < phraseFirstWordPhonemeArray.Length); i++)
                {
                    if (i > 0)
                        altBestResult += '_';
                    altBestResult += phraseFirstWordPhonemeArray[i];
                }

                // add it to diversityList
                if (!diversityList.Contains<string>(altBestResult))
                {
                    diversityList.Add(altBestResult);
                }
            }

            /* fill in phoneme hashtable. Using all the alternatives */
            for (int i = 0; i < result.Alternates.Count; i++)
            {
                RecognizedPhrase rp = result.Alternates[i];
                
                Double currentConfidence, oldConfidence;
                System.Diagnostics.Debug.WriteLine("alternative:" + i);
                for (int j = 0; ((j < numberOfMinRecognizedWord) && (j < rp.Words.Count)); j++)
                {
                    currentConfidence = 0;
                    /* If there is only one word, use the confidence of the entire sentence. */
                    if (rp.Words.Count == 1)
                    {
                        currentConfidence = rp.Confidence;
                    }
                    else
                    {
                        currentConfidence = rp.Words[j].Confidence;
                    }

                    if (!wordToConfidenceHash[j].ContainsKey(rp.Words[j].Text))
                    {
                        System.Diagnostics.Debug.WriteLine("entering: " + j);
                        wordToConfidenceHash[j][rp.Words[j].Text] = currentConfidence;
                    }

                    // if already present, update confidence score to the higher score
                    else
                    {
                        oldConfidence = (Double)wordToConfidenceHash[j][rp.Words[j].Text];
                        //if (currentConfidence > oldConfidence)
                        wordToConfidenceHash[j][rp.Words[j].Text] = oldConfidence + currentConfidence;
                        System.Diagnostics.Debug.WriteLine("updating: " + j);
                    }

                    System.Diagnostics.Debug.WriteLine("wordToConfidenceHash[j].Count = " + wordToConfidenceHash[j].Count);
                }

            }
           

            
        }

        static bool UnorderedEqual<T>(ICollection<T> a, ICollection<T> b)
        {
            // 1
            // Require that the counts are equal
            if (a.Count != b.Count)
            {
                return false;
            }
            // 2
            // Initialize new Dictionary of the type
            Dictionary<T, int> d = new Dictionary<T, int>();
            // 3
            // Add each key's frequency from collection A to the Dictionary
            foreach (T item in a)
            {
                int c;
                if (d.TryGetValue(item, out c))
                {
                    d[item] = c + 1;
                }
                else
                {
                    d.Add(item, 1);
                }
            }
            // 4
            // Add each key's frequency from collection B to the Dictionary
            // Return early if we detect a mismatch
            foreach (T item in b)
            {
                int c;
                if (d.TryGetValue(item, out c))
                {
                    if (c == 0)
                    {
                        return false;
                    }
                    else
                    {
                        d[item] = c - 1;
                    }
                }
                else
                {
                    // Not in dictionary
                    return false;
                }
            }
            // 5
            // Verify that all frequencies are zero
            foreach (int v in d.Values)
            {
                if (v != 0)
                {
                    return false;
                }
            }
            // 6
            // We know the collections are equal
            return true;
        }

    }


}
