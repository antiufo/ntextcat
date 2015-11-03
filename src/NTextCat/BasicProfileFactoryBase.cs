using System;
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

        public T LoadBinary(Stream inputStream, Func<LanguageModel<ValueString>, bool> filterPredicate = null)
        {
            filterPredicate = filterPredicate ?? (_ => true);
          


            using (var br = new BinaryReader(inputStream, Encoding.UTF8))
            {
                var l = new List<LanguageModel<ValueString>>();


                if (br.ReadInt32() != 1) throw new InvalidDataException();
                var maximumSizeOfDistribution = br.ReadInt32();
                var maxNGramLength = br.ReadInt32();

                while (true)
                {
                    if (br.ReadInt32() != 1) break;

                    var name2 = br.ReadString();
                    var name3 = br.ReadString();
                    var totalNoiseCount = br.ReadInt32();
                    var distinctNoiseCount = br.ReadInt32();
                    var count = br.ReadInt32();

                    var language = new LanguageInfo(name2, name3, null, null);

                    var features = new Distribution<ValueString>(new Bag<ValueString>());
                        
                    
                 
                    for (int i = 0; i < count; i++)
                    {
                        var text = br.ReadString();
                        var cnt = br.ReadInt32();
                        features.AddEvent(text, cnt);

                    }
                    features.AddNoise(totalNoiseCount, distinctNoiseCount);
                    var lm = new LanguageModel<ValueString>(features, language, new Dictionary<string, string>());


                    if (filterPredicate(lm)) l.Add(lm);
                }


                return Create(l, maxNGramLength, maximumSizeOfDistribution, OccuranceNumberThreshold, OnlyReadFirstNLines);


            }


        }
    }
}
