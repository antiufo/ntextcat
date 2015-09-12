using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IvanAkcheurov.NClassify;
using Shaman.Runtime;

namespace NTextCat
{
    /// <summary>
    /// Identifies the language of a given text.
    /// Please use <see cref="RankedLanguageIdentifierFactory"/> to load an instance of this class from a file
    /// </summary>
    public class RankedLanguageIdentifier
    {
        //private readonly List<LanguageModel<string>> _languageModels;
        private RankedClassifier<ValueString> _classifier;

        public int MaxNGramLength { get; private set; }
        public int MaximumSizeOfDistribution { get; private set; }
        public int OccuranceNumberThreshold { get; set; }
        public int OnlyReadFirstNLines { get; set; }
        private List<LanguageModel<ValueString>> _languageModels = new List<LanguageModel<ValueString>>();

        public IEnumerable<LanguageModel<ValueString>> LanguageModels
        {
            get { return _languageModels; }
        }

        public RankedLanguageIdentifier(IEnumerable<LanguageModel<ValueString>> languageModels, int maxNGramLength, int maximumSizeOfDistribution, int occuranceNumberThreshold, int onlyReadFirstNLines)
        {
            MaxNGramLength = maxNGramLength;
            MaximumSizeOfDistribution = maximumSizeOfDistribution;
            OccuranceNumberThreshold = occuranceNumberThreshold;
            OnlyReadFirstNLines = onlyReadFirstNLines;

            //_languageModels = languageModels.ToList();

            //_classifier =
            //    new KnnMonoCategorizedClassifier<IDistribution<string>, LanguageInfo>(
            //        (IDistanceCalculator<IDistribution<string>>) new RankingDistanceCalculator<IDistribution<string>>(MaximumSizeOfDistribution),
            //        _languageModels.ToDictionary(lm => lm.Features, lm => lm.Language));

            _classifier = new RankedClassifier<ValueString>(MaximumSizeOfDistribution);
            foreach (var languageModel in languageModels)
            {
                _classifier.AddEtalonLanguageModel(languageModel);
                _languageModels.Add(languageModel);
            }
        }


        public IEnumerable<Tuple<LanguageInfo, double>> Identify(ValueString text)
        {
            var extractor = new CharacterNGramExtractor(MaxNGramLength, OnlyReadFirstNLines);
            var tokens = extractor.GetFeatures(text);
            var model = LanguageModelCreator.CreateLangaugeModel(tokens, OccuranceNumberThreshold, MaximumSizeOfDistribution);
            var likelyLanguages = _classifier.Classify(model);
            return likelyLanguages;
        }
        public IEnumerable<Tuple<LanguageInfo, double>> IdentifyAlreadyUnderscoreSeparated(ValueString text)
        {
            var extractor = new CharacterNGramExtractor(MaxNGramLength, OnlyReadFirstNLines);
            var tokens = extractor.GetFeaturesAlreadyUnderscoreSeparated(text);
            var model = LanguageModelCreator.CreateLangaugeModel(tokens, OccuranceNumberThreshold, MaximumSizeOfDistribution);
            var likelyLanguages = _classifier.Classify(model);
            return likelyLanguages;
        }
    }
}
