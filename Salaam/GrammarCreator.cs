using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Microsoft.Speech.Recognition;

namespace Salaam
{
    public class GrammarCreator
    {
        // number of phonemes in this language
        private static int number_of_phonemes = 37;
        
        // array of phonemes in this language
        public static string[] enusphoneme = {"AA", "AH", "AI", "AO", "AX", "EH", "EI",
                                               "I", "IH", "O", "U", "UH", "B", "CH", "D",
                                               "DH", "F", "G", "H", "J", "JH", "K", "L",
                                               "M", "N", "NG", "P", "R", "RA", "S", "SH",
                                               "T", "TH", "V", "W", "Z", "ZH"};

        // Grammar object
        private Grammar gr;

        // full path of the grxml file
        public string grammarPath { get; set; }
        // directory of the grxml file
        public string grammarDir { get; set; }
        // file name of the grxml file
        public string grammarFileName { get; set; }
        // rule name used in the grammar
        public string grammarRuleName { get; set; }

        // basic constructor
        public GrammarCreator(string grammar_dir, string grammar_file_name, string grammar_rule_name)
        {
            grammarDir = grammar_dir;
            grammarFileName = grammar_file_name + ".grxml";
            grammarPath = grammarDir + "\\" + grammarFileName;
            grammarRuleName = grammar_rule_name;
            gr = null;
        }

        /// <summary>
        /// Removes .grxml file created
        /// </summary>
        public void Destroy()
        {
            File.Delete(grammarPath);
        }

        /// <summary>
        /// Creates the Grammar based on the path and rule name
        /// </summary>
        /// <returns>Grammar object used in SpeechRecognizer</returns>
        public Grammar getGrammar()
        {
            FileStream grammar_filestream = new FileStream(grammarPath, FileMode.Open, FileAccess.Read);
            gr = new Grammar(grammar_filestream, grammarRuleName);
            if (gr == null) System.Diagnostics.Debug.WriteLine("Grammar is non-existent");
            grammar_filestream.Close();
          
            return gr;
        }

        /// <summary>
        /// Builds a .grxml file that references another rule in another .grxml file.
        /// The new file allows repeats of the referenced rule for lower_bound to upper_bound number of times
        /// </summary>
        /// <param name="reference_rule_path">Full reference to other rule of the form "grammar.grxml#otherRule" </param>
        /// <param name="lower_bound">Minimum number of times to repeat the referenced rule </param>
        /// <param name="upper_bound">Maximum number of times to repeat the referenced rule </param>
        public void buildGrammarFileWithRepeats(string reference_rule_path, int lower_bound, int upper_bound)
        {
            boundgr(true);
            boundrule(true, grammarRuleName);
            LogGrammar("<item repeat=\"" + lower_bound + "-" + upper_bound + "\"><ruleref uri=\"" + reference_rule_path
                + "\"/></item>");
            boundrule(false, "");
            boundgr(false);
        }

        
       

       
        /// <summary>
        /// Builds a .grxml file that takes in a List of strings and uses the strings as items in the .grxml file
        /// </summary>
        /// <param name="result">List of strings that represent items</param>
        public void buildGrammarFileWithItems(List<string> result)
        {
            boundgr(true);
            buildRuleWithItems(grammarRuleName, result);
            boundgr(false);
        }

        /// <summary>
        /// Builds a .grxml file with multiple rules as specified in rules with names in rule_names
        /// </summary>
        /// <param name="rule_names"> List of rule names </param>
        /// <param name="rules"> List of rules, which are List of items </param>
        public void buildGrammarFileWithMultipleRules(List<string> rule_names, List<List<string>> rules)
        {
            boundgr(true);
  
            for (int i = 0; i < rule_names.Count(); i++)
            {
                boundrule(true, rule_names[i]);
                for(int j = 0; j < rules[i].Count(); j++)
                {
                    LogGrammar(rules[i][j]);
                }
                boundrule(false, "");
            }
            
            boundgr(false);
        }

        /// <summary> 
        /// allcombinations(curr, start_num) returns an list of strings consisting of 
        /// curr_A, curr_A_..._A, ... , curr_Z_..._Z
        /// of length from 1 to max_len 
        /// </summary>
        ///
        /// <param name="curr"> current string to be appended to </param>
        /// <param name="start_num"> index of phoneme to begin </param>
        /// <param name="max_len"> maximum length of the MSS word </param>
        public List<string> allcombinations(string curr, int start_num, int max_len)
        {
            if (curr.Split('_').Count() == max_len && curr != "") return new List<string>();

            List<string> result = new List<string>();
            
            for (int i = start_num; i < number_of_phonemes; i++)
            {
                string temp;
                if (curr.Equals(""))
                    temp = enusphoneme[i].ToString();
                else
                {
                    temp = curr.ToString() + "_" + enusphoneme[i].ToString();
                }
                result.Add(temp);
                result.AddRange(allcombinations(temp, 0, max_len));

            }
            return result;
        }

        /// <summary> 
        /// combination(curr, start_num) returns an list of strings consisting of 
        /// curr_A_..._A, ... , curr_Z_..._Z
        /// of length max_len 
        /// </summary>
        ///
        /// <param name="curr"> current string to be appended to </param>
        /// <param name="start_num"> index of phoneme to begin </param>
        /// <param name="max_len"> fixed length of the MSS word </param>
        public List<string> combination(string curr, int start_num, int max_len)
        {

            List<string> result = new List<string>();
            if (curr.Split('_').Count() == max_len)
            {
                result.Add(curr);
                return result;

            }

            for (int i = start_num; i < number_of_phonemes; i++)
            {
                string temp;
                if (curr.Equals(""))
                    temp = enusphoneme[i].ToString();
                else
                    temp = curr.ToString() + "_" + enusphoneme[i].ToString();

                result.AddRange(combination(temp, 0, max_len));

            }
            return result;
        }
        

         /// <summary>
         /// Outputs the string to the .grxml file.
         /// </summary>
         /// <param name="txt"> Output string. </param>
         public void LogGrammar(string txt)
         {
             File.AppendAllText(grammarPath, txt + "\r\n");
         }

        /// <summary>
        /// Adds the grammar xmlns tag
        /// </summary>
        /// <param name="isStart">indicate if it is the head tag</param>
         public void boundgr(bool isStart)
         {
             string output;
             if (isStart)
             {
                 File.Delete(grammarPath);
                 output = @"<grammar xmlns:sapi='http://schemas.microsoft.com/Speech/2002/06/SRGSExtensions' xml:lang='en-US' tag-format='semantics-ms/1.0' version='1.0' mode='voice' xmlns='http://www.w3.org/2001/06/grammar' sapi:alphabet='x-microsoft-ups'>
";
             }
             else
             {
                 output = @"
</grammar>
";
             }
             LogGrammar(output);
         }
        
        /// <summary>
        /// Adds the rule id tag
        /// </summary>
        /// <param name="isStart">indicate if it is the head tag</param>
        /// <param name="rname"> rule name </param>
         public void boundrule(bool isStart, string rname)
         {
             string output;
             if (isStart)
             {
                 output = "<rule id=\"" + rname + @""" scope=""public"">
		<item>
			<?MS_Grammar_Editor GroupWrap?>
			<one-of>
";
             }
             else
             {
                 output = @"			</one-of>
		</item>
	</rule>
";
             }
             LogGrammar(output);
         }

         public void boundItem(bool isStart)
         {
             string output;
             if (isStart)
             {
                 output = "<item>";
             }
             else
             {
                 output = "</item>";
             }
             LogGrammar(output);
         }

         /// <summary>
         /// Set the file name portion of the GrammarPath.
         /// </summary>
         /// <param name="newfname"> New file name of the output grammar. </param>
         protected void setGrammarPath(string newfname)
         {
             grammarFileName = newfname;
             grammarPath = grammarDir + "\\" + grammarFileName + ".grxml";
         }

         public void buildRuleWithItems(string rule_name, List<string> items)
         {
             boundrule(true, rule_name);
             
             for (int i = 0; i < items.Count(); i++)
             {
                 LogGrammar("<item><token sapi:pron=\"" + items[i].Replace('_', ' ') + "\">" + items[i] + "</token></item>");
             }
             boundrule(false, "");
         }


         public void buildRuleWithRepeats(string grammar_rule_name, string reference_rule_path, int lower_bound, int upper_bound)
         {
             boundrule(true, grammar_rule_name);
             LogGrammar("<item repeat=\"" + lower_bound + "-" + upper_bound + "\"><ruleref uri=\"" + reference_rule_path
                 + "\"/></item>");
             boundrule(false, "");
         }

         public void buildItemWithRepeats(string reference_rule_path, int lower_bound, int upper_bound)
         {
             
             LogGrammar("<item repeat=\"" + lower_bound + "-" + upper_bound + "\"><ruleref uri=\"" + reference_rule_path
                 + "\"/></item>");
             
         }

         public void buildRuleRef(string reference_rule_path)
         {
             LogGrammar("<ruleref uri=\"" + reference_rule_path + "\" />");
         }

         public void buildItem(string reference_rule_path)
         {

             LogGrammar("<item><ruleref uri=\"" + reference_rule_path
                 + "\"/></item>");

         }

         public List<string> tagAsItems(List<string> items)
         {
             List<string> result = new List<string>();
             for (int i = 0; i < items.Count(); i++)
             {
                 result.Add("<item><token sapi:pron=\"" + items[i].Replace('_', ' ') + "\">" + items[i] + "</token></item>");
             }
             return result;
             
         }

    }
}
