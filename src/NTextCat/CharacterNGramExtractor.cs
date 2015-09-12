using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using IvanAkcheurov.NClassify;
using Shaman.Runtime;

namespace NTextCat
{
    /// <summary>
    /// Extracts char-ngrams out of TextReader, char[] or string.
    /// </summary>
    public class CharacterNGramExtractor : IFeatureExtractor<TextReader, ValueString>, IFeatureExtractor<char[], ValueString>, IFeatureExtractor<ValueString, ValueString>
    {
        private readonly int _maxNGramLength = 5;
        private readonly long _maxLinesToRead;

        public CharacterNGramExtractor(int maxNGramLength, long maxLinesToRead = long.MaxValue)
        {
            if (maxNGramLength <= 0)
                throw new ArgumentOutOfRangeException("maxNGramLength", "should be positive integer number");
            _maxNGramLength = maxNGramLength;
            _maxLinesToRead = maxLinesToRead;
        }

        /// <summary>
        /// Splits text into tokens, transforms each "token" into "_token_" (prepends and appends underscores) 
        /// and then extracts proper ngrams out of each "_token_".
        /// </summary>
        /// <param name="text"></param>
        /// <returns>the sequence of ngrams extracted</returns>
        public IEnumerable<ValueString> GetFeatures(ValueString text)
        {
            return GetFeatures(new StringReader(text));
        }

        /// <summary>
        /// Splits text into tokens, transforms each "token" into "_token_" (prepends and appends underscores) 
        /// and then extracts proper ngrams out of each "_token_".
        /// </summary>
        /// <param name="text"></param>
        /// <returns>the sequence of ngrams extracted</returns>
        public IEnumerable<ValueString> GetFeatures(char[] text)
        {
            return GetFeatures(new string(text));
        }


        private class CharQueue
        {
            public ValueString InnerString;
            internal int Count;
            internal int Start;

            internal void Clear()
            {
                Count = 0;
            }

            internal char Dequeue()
            {
                Count--;
                return InnerString[Start++];
            }

            internal char Peek()
            {
                return InnerString[Start];
            }

            internal ValueString ToValueString()
            {
                return InnerString.Substring(Start, Count);
            }

            internal void Enqueue(char v)
            {
                if (InnerString[Start + Count] != v)
                    throw new InvalidOperationException();
                Count++;
            }
        }

        public IEnumerable<ValueString> GetFeaturesAlreadyUnderscoreSeparated(ValueString text)
        {
            long numberOfLinesRead = 0;
            var currentNgrams = Enumerable.Range(0, _maxNGramLength).Select(_ => new CharQueue() { InnerString = text }).ToArray();
            bool insideWord = false;

            char previousByte = (char)0;
            var charsToProcess = new CharQueue() { InnerString = text };
            for (int i = 0; i < text.Length; i++)
            {
                // here we have explicitly implemented transforming "abcdefg" into "_abcdefg_" and getting <1.._maxNGramLength>grams
                char currentByte = text[i];
                if (currentByte == 0xD || currentByte == 0xA && previousByte != 0xD)
                    numberOfLinesRead++;
                if (numberOfLinesRead >= _maxLinesToRead)
                    break;

                bool cleanNgrams = false;

                if (insideWord)
                {
                    if (currentByte == '_')
                    {
                        insideWord = false;
                        if (charsToProcess.Count == 0) charsToProcess.Start = i;
                        charsToProcess.Enqueue('_');
                        cleanNgrams = true;
                    }
                    else
                    {
                        charsToProcess.Enqueue(currentByte);
                    }

                }
                else
                {
                    if (currentByte == '_')
                    {
                        // skip it;
                    }
                    else
                    {
                        insideWord = true;
                        charsToProcess.Start = i - 1;
                        charsToProcess.Enqueue('_');
                        charsToProcess.Enqueue(currentByte);
                    }
                }

                foreach (var ngram in UpdateAndProduceNgrams(charsToProcess, currentNgrams))
                    yield return ngram;

                if (cleanNgrams)
                {
                    foreach (var ngram in currentNgrams)
                    {
                        ngram.Clear();
                    }
                }
                previousByte = currentByte;
            }

            if (insideWord)
            {
                charsToProcess.Enqueue('_');
                foreach (var ngram in UpdateAndProduceNgrams(charsToProcess, currentNgrams))
                    yield return ngram;
            }
        }


        /// <summary>
        /// Splits text into tokens, transforms each "token" into "_token_" (prepends and appends underscores) 
        /// and then extracts proper ngrams out of each "_token_".
        /// </summary>
        /// <param name="text"></param>
        /// <returns>the sequence of ngrams extracted</returns>
        public IEnumerable<ValueString> GetFeatures(TextReader text)
        {
            long numberOfLinesRead = 0;
            var currentNgrams = Enumerable.Range(0, _maxNGramLength).Select(_ => new Queue<char>()).ToArray();
            bool insideWord = false;
            var charsToProcess = new Queue<char>();
            var buffer = new char[4096];
            int charsRead;
            char previousByte = (char)0;
            while ((charsRead = text.Read(buffer, 0, buffer.Length)) > 0)
            {
                for (int i = 0; i < charsRead; i++)
                {
                    // here we have explicitly implemented transforming "abcdefg" into "_abcdefg_" and getting <1.._maxNGramLength>grams
                    char currentByte = buffer[i];
                    if (currentByte == 0xD || currentByte == 0xA && previousByte != 0xD)
                        numberOfLinesRead++;
                    if (numberOfLinesRead >= _maxLinesToRead)
                        break;

                    bool cleanNgrams = false;

                    if (insideWord)
                    {
                        if (IsSeparator(currentByte))
                        {
                            insideWord = false;
                            charsToProcess.Enqueue('_');
                            cleanNgrams = true;
                        }
                        else
                        {
                            charsToProcess.Enqueue(currentByte);
                        }

                    }
                    else
                    {
                        if (IsSeparator(currentByte))
                        {
                            // skip it;
                        }
                        else
                        {
                            insideWord = true;
                            charsToProcess.Enqueue('_');
                            charsToProcess.Enqueue(currentByte);
                        }
                    }

                    foreach (var ngram in UpdateAndProduceNgrams(charsToProcess, currentNgrams))
                        yield return ngram;

                    if (cleanNgrams)
                    {
                        foreach (Queue<char> ngram in currentNgrams)
                        {
                            ngram.Clear();
                        }
                    }
                    previousByte = currentByte;
                }
            }
            if (insideWord)
            {

                charsToProcess.Enqueue('_');
                foreach (var ngram in UpdateAndProduceNgrams(charsToProcess, currentNgrams))
                    yield return ngram;
            }
        }


        private IEnumerable<ValueString> UpdateAndProduceNgrams(CharQueue charsToProcess, CharQueue[] currentNgrams)
        {
            while (charsToProcess.Count > 0)
            {
                var processingByte = charsToProcess.Dequeue();
                for (int j = 0; j < currentNgrams.Length; j++)
                {
                    var currentNgram = currentNgrams[j];
                    // if ngram is complete (e.g. 3gram contains 3 characters)
                    if (currentNgram.Count > j)
                        currentNgram.Dequeue();
                    if (currentNgram.Count == 0) currentNgram.Start = charsToProcess.Start - 1;
                    currentNgram.Enqueue(processingByte);
                    // if ngram is complete (e.g. 3gram contains 3 characters)
                    if (j == 0) // if unigram
                    {
                        var ch = currentNgram.Peek();
                        // prevent pure "_" as ngram otherwise it becomes the most frequent ngram
                        if (ch != '_')
                            yield return currentNgram.ToValueString().Substring(1);
                    }
                    else if (currentNgram.Count > j)
                    {
                        // todo: optimization to remove excessive array creation: Queue => _Array_ => String
                        yield return currentNgram.ToValueString();
                    }
                }
            }
        }

        private IEnumerable<string> UpdateAndProduceNgrams(Queue<char> charsToProcess, Queue<char>[] currentNgrams)
        {
            while (charsToProcess.Count > 0)
            {
                var processingByte = charsToProcess.Dequeue();
                for (int j = 0; j < currentNgrams.Length; j++)
                {
                    var currentNgram = currentNgrams[j];
                    // if ngram is complete (e.g. 3gram contains 3 characters)
                    if (currentNgram.Count > j)
                        currentNgram.Dequeue();
                    currentNgram.Enqueue(processingByte);
                    // if ngram is complete (e.g. 3gram contains 3 characters)
                    if (j == 0) // if unigram
                    {
                        var ch = currentNgram.Peek();
                        // prevent pure "_" as ngram otherwise it becomes the most frequent ngram
                        if (ch != '_')
                            yield return new string(ch, 1);
                    }
                    else if (currentNgram.Count > j)
                    {
                        // todo: optimization to remove excessive array creation: Queue => _Array_ => String
                        yield return new string(currentNgram.ToArray());
                    }
                }
            }
        }

        private static bool IsSeparator(char b)
        {
            return Char.IsLetter(b) == false;
        }
    }
}
