﻿using Shaman.Runtime;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace NTextCat
{
    /// <summary>
    /// Loads an instance of <see cref="NaiveBayesLanguageIdentifier"/> from file or trains a new instance out of a data set.
    /// </summary>
    public class NaiveBayesLanguageIdentifierFactory : BasicProfileFactoryBase<NaiveBayesLanguageIdentifier>
    {
#if !PORTABLE
        public NaiveBayesLanguageIdentifierFactory()
        {
        }
#endif

        public NaiveBayesLanguageIdentifierFactory(int maxNGramLength, int maximumSizeOfDistribution, int occuranceNumberThreshold, int onlyReadFirstNLines, bool allowMultithreading)
            : base(maxNGramLength, maximumSizeOfDistribution, occuranceNumberThreshold, onlyReadFirstNLines, allowMultithreading)
        {
        }

        public override NaiveBayesLanguageIdentifier Create(IEnumerable<LanguageModel<ValueString>> languageModels, int maxNGramLength, int maximumSizeOfDistribution, int occuranceNumberThreshold, int onlyReadFirstNLines)
        {
            var result = new NaiveBayesLanguageIdentifier(languageModels, maxNGramLength, onlyReadFirstNLines);
            return result;
        }
    }
}
