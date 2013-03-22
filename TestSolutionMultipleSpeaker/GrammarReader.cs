using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Speech.Recognition;
using Microsoft.Speech.Recognition.SrgsGrammar;

namespace TestSolutionMultipleSpeaker
{
    public class GrammarReader
    {
        public string language { get; set; }
        public string directory { get; set; }
        public string ruleName { get; set; }

        public GrammarReader(string lang, string dir, string rule_name)
        {
            language = lang;
            directory = dir;
            ruleName = rule_name;
        }

        private SrgsDocument getSrgs(List<int> speaker_list, List<int> sample_list)
        {
            string speaker = "speaker-";
            foreach (int speakerIndex in speaker_list)
            {
                speaker += speakerIndex + "_";
            }
            string sample = "sample-";
            foreach (int sampleIndex in sample_list)
            {
                sample += sampleIndex + "_";

            }
            return new SrgsDocument(directory + "\\" + language + speaker + sample + ".grxml");
        }

        private SrgsDocument getSrgsDiscriminative(List<int> speaker_list, List<int> sample_list, int speakerNum)
        {
            string speaker = "speaker-";
            foreach (int speakerIndex in speaker_list)
            {
                speaker += speakerIndex + "_";
            }
            string sample = "sample-";
            foreach (int sampleIndex in sample_list)
            {
                sample += sampleIndex + "_";

            }
            System.Diagnostics.Debug.WriteLine(directory + "\\" + language + speaker + sample + speakerNum + ".grxml");
            return new SrgsDocument(directory + "\\" + language + speaker + sample + speakerNum + ".grxml");
        }

        public string getFileName(List<int> speaker_list, List<int> sample_list)
        {
            string speaker = "speaker-";
            foreach (int speakerIndex in speaker_list)
            {
                speaker += speakerIndex + "_";
            }
            string sample = "sample-";
            foreach (int sampleIndex in sample_list)
            {
                sample += sampleIndex + "_";

            }
            return language + speaker + sample;
        }

        public Dictionary<string, List<string>> outputDiscriminativeEliminative(List<int> speaker_list, List<int> sample_list, int speakerNum)
        {
            //List<string> result = new List<string>();
            Dictionary<string, List<string>> result = new Dictionary<string, List<string>>();
            SrgsDocument prttest = getSrgsDiscriminative(speaker_list, sample_list, speakerNum);
            foreach (SrgsRule curr_rule in prttest.Rules)
            {
                if (curr_rule.Id.Equals(ruleName))
                {
                    foreach (SrgsItem curr_out in curr_rule.Elements)
                    {
                        foreach (SrgsOneOf curr_1o in curr_out.Elements)
                        {
                            foreach (SrgsItem curr_i in curr_1o.Items)
                            {
                                foreach (SrgsToken curr_t in curr_i.Elements)
                                {
                                    string[] word = curr_t.Text.Split('_');
                                    if (!result.ContainsKey(word[0]))
                                    {
                                        result[word[0]] = new List<string>();
                                    }

                                    result[word[0]].Add(curr_t.Pronunciation + "_" + word[1]);
                                    //result.Add("<item><token sapi:pron=\"" + curr_t.Pronunciation + "\">" + curr_t.Text + "</token></item>");
                                }

                            }
                        }
                    }
                    break;
                }
            }
            return result;
        }

        public Dictionary<string, List<string>> outputDiscriminative(List<int> speaker_list, List<int> sample_list, int speakerNum)
        {
            //List<string> result = new List<string>();
            Dictionary<string, List<string>> result = new Dictionary<string, List<string>>();
            SrgsDocument prttest = getSrgsDiscriminative(speaker_list, sample_list, speakerNum);
            foreach (SrgsRule curr_rule in prttest.Rules)
            {
                if (curr_rule.Id.Equals(ruleName))
                {
                    foreach (SrgsItem curr_out in curr_rule.Elements)
                    {
                        foreach (SrgsOneOf curr_1o in curr_out.Elements)
                        {
                            foreach (SrgsItem curr_i in curr_1o.Items)
                            {
                                foreach (SrgsToken curr_t in curr_i.Elements)
                                {
                                    string[] word = curr_t.Text.Split('_');
                                    if (!result.ContainsKey(word[0]))
                                    {
                                        result[word[0]] = new List<string>();
                                    }

                                    result[word[0]].Add(curr_t.Pronunciation);
                                    //result.Add("<item><token sapi:pron=\"" + curr_t.Pronunciation + "\">" + curr_t.Text + "</token></item>");
                                }

                            }
                        }
                    }
                    break;
                }
            }
            return result;
        }

        public Dictionary<string, List<string>> output(List<int> speaker_list, List<int> sample_list)
        {
            //List<string> result = new List<string>();
            Dictionary<string, List<string>> result = new Dictionary<string, List<string>>();
            SrgsDocument prttest = getSrgs(speaker_list, sample_list);
            foreach (SrgsRule curr_rule in prttest.Rules)
            {
                if (curr_rule.Id.Equals(ruleName))
                {
                    foreach (SrgsItem curr_out in curr_rule.Elements)
                    {
                        foreach (SrgsOneOf curr_1o in curr_out.Elements)
                        {
                            foreach (SrgsItem curr_i in curr_1o.Items)
                            {
                                foreach (SrgsToken curr_t in curr_i.Elements)
                                {
                                    string[] word = curr_t.Text.Split('_');
                                    if(!result.ContainsKey(word[0]))
                                    {
                                        result[word[0]] = new List<string>();
                                    }
                                   
                                    result[word[0]].Add(curr_t.Pronunciation);
                                    //result.Add("<item><token sapi:pron=\"" + curr_t.Pronunciation + "\">" + curr_t.Text + "</token></item>");
                                }
                                
                            }
                        }
                    }
                    break;
                }
            }
            return result;
        }

        

    }
}
