﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IvanAkcheurov.NClassify;
using Shaman.Runtime;

namespace NTextCat
{
    public class NaiveBayesLanguageIdentifier
    {
        public int MaxNGramLength { get; private set; }
        public int OnlyReadFirstNLines { get; set; }
        private NaiveBayesClassifier<IEnumerable<ValueString>, ValueString, LanguageInfo> _classifier;

        public NaiveBayesLanguageIdentifier(IEnumerable<LanguageModel<ValueString>> languageModels,  int maxNGramLength, int onlyReadFirstNLines)
        {
            MaxNGramLength = maxNGramLength;
            OnlyReadFirstNLines = onlyReadFirstNLines;
            _classifier = new NaiveBayesClassifier<IEnumerable<ValueString>, ValueString, LanguageInfo>(
                languageModels.ToDictionary(lm => lm.Language, lm => lm.Features), 1);
        }

        public IEnumerable<Tuple<LanguageInfo, double>> Identify(string text)
        {
            var extractor = new CharacterNGramExtractor(MaxNGramLength, OnlyReadFirstNLines);
            var tokens = extractor.GetFeatures(text);
            var likelyLanguages = _classifier.Classify(tokens);
            return likelyLanguages;
        }
    }
}
