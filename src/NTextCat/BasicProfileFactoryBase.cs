﻿using System;
using System.Collections.Generic;
#if !PORTABLE
using System.Configuration;
#endif
using System.IO;
using System.Linq;
using System.Text;
using IvanAkcheurov.NClassify;
using Shaman.Runtime;

namespace NTextCat
{
    public abstract class BasicProfileFactoryBase<T>
    {
        public int MaxNGramLength { get; private set; }
        public int MaximumSizeOfDistribution { get; private set; }
        public int OccuranceNumberThreshold { get; private set; }
        public int OnlyReadFirstNLines { get; private set; }
        /// <summary>
        /// true if it is allowed to use more than one thread for training
        /// </summary>
        public bool AllowUsingMultipleThreadsForTraining { get; private set; }

#if !PORTABLE
        public static TSetting GetSetting<TSetting>(string key, TSetting defaultValue)
        {
            var setting = ConfigurationManager.AppSettings[key];
            if (setting == null)
                return defaultValue;
            var result = (TSetting)Convert.ChangeType(setting, typeof(TSetting), System.Globalization.CultureInfo.InvariantCulture);
            return result;
        }

        public BasicProfileFactoryBase()
            : this(5, GetSetting("MaximumSizeOfDistribution", 4000), GetSetting("OccuranceNumberThreshold", 0), int.MaxValue)
        {
        }
#endif
        public BasicProfileFactoryBase(int maxNGramLength, int maximumSizeOfDistribution, int occuranceNumberThreshold, int onlyReadFirstNLines, bool allowUsingMultipleThreadsForTraining = true)
        {
            MaxNGramLength = maxNGramLength;
            MaximumSizeOfDistribution = maximumSizeOfDistribution;
            OccuranceNumberThreshold = occuranceNumberThreshold;
            OnlyReadFirstNLines = onlyReadFirstNLines;
            AllowUsingMultipleThreadsForTraining = allowUsingMultipleThreadsForTraining;
        }

        public T Create(IEnumerable<LanguageModel<ValueString>> languageModels)
        {
            return Create(languageModels, MaxNGramLength, MaximumSizeOfDistribution, OccuranceNumberThreshold, OnlyReadFirstNLines);
        }
        public abstract T Create(IEnumerable<LanguageModel<ValueString>> languageModels, int maxNGramLength, int maximumSizeOfDistribution, int occuranceNumberThreshold, int onlyReadFirstNLines);

        public T Train(IEnumerable<Tuple<LanguageInfo, TextReader>> input)
        {
            var languageModels = TrainModels(input).ToList();
            var identifier = Create(languageModels);
            return identifier;
        }

        /// <summary>
        /// Disposes TextReader instances!
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public IEnumerable<LanguageModel<ValueString>> TrainModels(IEnumerable<Tuple<LanguageInfo, TextReader>> input)
        {
            if (AllowUsingMultipleThreadsForTraining)
            {
                return input.AsParallel().AsOrdered()
                    .Select(
                        languageAndText =>
                        {
                            using (languageAndText.Item2)
                            {
                                return TrainModel(languageAndText.Item1, languageAndText.Item2);
                            }
                        });
            }
            return input.Select(
                languageAndText =>
                {
                    using (languageAndText.Item2)
                    {
                        return TrainModel(languageAndText.Item1, languageAndText.Item2);
                    }
                });
        }

        private LanguageModel<ValueString> TrainModel(LanguageInfo languageInfo, TextReader text)
        {
            IEnumerable<ValueString> tokens = new CharacterNGramExtractor(MaxNGramLength, OnlyReadFirstNLines).GetFeatures(text);
            IDistribution<ValueString> distribution = LanguageModelCreator.CreateLangaugeModel(tokens, OccuranceNumberThreshold, MaximumSizeOfDistribution);
            var languageModel = new LanguageModel<ValueString>(distribution, languageInfo);
            return languageModel;
        }
#if !PORTABLE
        public void SaveProfile(IEnumerable<LanguageModel<ValueString>> languageModels, string outputFilePath)
        {
            using (var file = File.OpenWrite(outputFilePath))
            {
                SaveProfile(languageModels, file);
            }
        }

        public void SaveProfile(IEnumerable<LanguageModel<ValueString>> languageModels, Stream outputStream)
        {
            XmlProfilePersister.Save(languageModels, MaximumSizeOfDistribution, MaxNGramLength, outputStream);
        }

        public T TrainAndSave(IEnumerable<Tuple<LanguageInfo, TextReader>> input, string outputFilePath)
        {
            using (var file = File.OpenWrite(outputFilePath))
            {
                return TrainAndSave(input, file);
            }
        }

        public T TrainAndSave(IEnumerable<Tuple<LanguageInfo, TextReader>> input, Stream outputStream)
        {
            var languageModels = TrainModels(input).ToList();
            SaveProfile(languageModels, outputStream);
            return Create(languageModels, MaxNGramLength, MaximumSizeOfDistribution, OccuranceNumberThreshold, OnlyReadFirstNLines);
        }

        public T Load(Func<LanguageModel<ValueString>, bool> filterPredicate = null)
        {
            var defaultProfile = GetSetting("LanguageIdentificationProfileFilePath", string.Empty);
            if (File.Exists(defaultProfile) == false)
                throw new InvalidOperationException("Cannot find a profile in the following path: '" + defaultProfile + "'");
            return Load(defaultProfile, filterPredicate);
        }

        public T Load(string inputFilePath, Func<LanguageModel<ValueString>, bool> filterPredicate = null)
        {
            using (var file = File.OpenRead(inputFilePath))
            {
                return Load(file, filterPredicate);
            }
        }
#endif
        public T Load(Stream inputStream, Func<LanguageModel<ValueString>, bool> filterPredicate = null)
        {
            filterPredicate = filterPredicate ?? (_ => true);
            int maxNGramLength;
            int maximumSizeOfDistribution;
            var languageModelList =
                XmlProfilePersister.Load<ValueString>(inputStream, out maximumSizeOfDistribution, out maxNGramLength)
                    .Where(filterPredicate);

            return Create(languageModelList, maxNGramLength, maximumSizeOfDistribution, OccuranceNumberThreshold, OnlyReadFirstNLines);
        }
    }
}
