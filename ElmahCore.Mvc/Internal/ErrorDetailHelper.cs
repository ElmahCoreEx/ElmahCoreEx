using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace ElmahCore.Mvc
{
    internal class SourceInfo
    {
        public string Type { get; set; }

        public string Method { get; set; }

        public string Source { get; set; }

        public int Line { get; set; }
    }

    internal static class ErrorDetailHelper
    {
        private static readonly Dictionary<string, StackFrameSourceCodeInfo> Cache =
            new Dictionary<string, StackFrameSourceCodeInfo>();

        // make it internal to enable unit testing
        internal static StackFrameSourceCodeInfo GetStackFrameSourceCodeInfo(string[] sourcePath, string method,
            string type, string filePath, int lineNumber)
        {
            var key = $"{method}:{type}:{filePath}:{lineNumber}";
            lock (Cache)
            {
                if (Cache.TryGetValue(key, out var info)) return info;
            }

            var stackFrame = new StackFrameSourceCodeInfo
            {
                Function = method,
                Type = type,
                File = filePath,
                FileName = Path.GetFileName(filePath),
                Line = lineNumber
            };

            if (string.IsNullOrEmpty(stackFrame.File)) return stackFrame;

            IEnumerable<string> lines = null;
            var path = GetPath(sourcePath, filePath);

            if (path != null) lines = File.ReadLines(path);

            if (lines != null) ReadFrameContent(stackFrame, lines, stackFrame.Line, stackFrame.Line);

            lock (Cache)
            {
                if (!Cache.ContainsKey(key)) Cache.Add(key, stackFrame);
            }

            return stackFrame;
        }

        private static string GetPath(string[] sourcePaths, string filePath)
        {
            sourcePaths ??= new[] { "" };
            foreach (var source in sourcePaths)
            {
                var sourcePath = source;
                var split = filePath.Split(Path.DirectorySeparatorChar);
                if (source != "" && !sourcePath.EndsWith(Path.DirectorySeparatorChar.ToString()))
                    sourcePath += Path.DirectorySeparatorChar;
                var curLen = 0;
                foreach (var subPath in split)
                {
                    var curPath = sourcePath + filePath.Substring(curLen);
                    if (File.Exists(curPath))
                        return curPath;
                    curLen += subPath.Length + 1;
                }
            }

            return null;
        }

        // make it internal to enable unit testing
        private static void ReadFrameContent(
            StackFrameSourceCodeInfo frame,
            IEnumerable<string> allLines,
            int errorStartLineNumberInFile,
            int errorEndLineNumberInFile)
        {
            // Get the line boundaries in the file to be read and read all these lines at once into an array.
            var preErrorLineNumberInFile = Math.Max(errorStartLineNumberInFile - 10, 1);
            var postErrorLineNumberInFile = errorEndLineNumberInFile + 10;
            var codeBlock = allLines
                .Skip(preErrorLineNumberInFile - 1)
                .Take(postErrorLineNumberInFile - preErrorLineNumberInFile + 1)
                .ToArray();

            var numOfErrorLines = errorEndLineNumberInFile - errorStartLineNumberInFile + 1;
            var errorStartLineNumberInArray = errorStartLineNumberInFile - preErrorLineNumberInFile;

            frame.PreContextLine = preErrorLineNumberInFile;
            frame.PreContextCode = string.Join(Environment.NewLine, codeBlock.Take(errorStartLineNumberInArray));
            frame.ContextCode = string.Join(Environment.NewLine, codeBlock
                .Skip(errorStartLineNumberInArray)
                .Take(numOfErrorLines));
            frame.PostContextCode = string.Join(Environment.NewLine, codeBlock
                .Skip(errorStartLineNumberInArray + numOfErrorLines));
        }

        public static (string markup, List<SourceInfo> srcList) MarkupStackTrace(string text)
        {
            var list = new List<SourceInfo>();

            (HtmlChunk File, HtmlChunk Line) SourceLocationSelector(HtmlChunk f, HtmlChunk l)
            {
                if (int.TryParse(l.Html, out var line))
                {
                    list.Add(new SourceInfo { Source = f.Html, Line = line });
                }

                return (
                    File: f.Html?.Length > 0
                        ? new HtmlChunk(f.Index, f.End, $"<span class='st-file'>{f.Html}</span>")
                        : new HtmlChunk(),
                    Line: l.Html?.Length > 0
                        ? new HtmlChunk(l.Index, l.End, $"<span class='st-line'>{l.Html}</span>")
                        : new HtmlChunk()
                );
            }

            IEnumerable<HtmlChunk> Selector(HtmlChunk f, (HtmlChunk Type, HtmlChunk Method) tm,
                (HtmlChunk List, (HtmlChunk Type, HtmlChunk Name)[] Parameters) p,
                (HtmlChunk File, HtmlChunk Line) fl) =>
                new[]
                    {
                        new[]
                        {
                            new HtmlChunk(f.Index, f.Index, "<span class='st-frame'>"),
                            tm.Type,
                            tm.Method,
                            new HtmlChunk(p.List.Index, p.List.Index, "<span class='params'>")
                        },
                        from pe in p.Parameters from e in new[] { pe.Type, pe.Name } select e,
                        new[]
                        {
                            new HtmlChunk(p.List.End, p.List.End, "</span>"), fl.File, fl.Line,
                            new HtmlChunk(f.End, f.End, "</span>")
                        }
                    }.SelectMany(tokens => tokens, (tokens, token) => new { tokens, token })
                    .Where(t => t.token.Html != null)
                    .Select(t => t.token);

            var frames = StackTraceParser.Parse
            (
                text,
                TokenSelector,
                MethodSelector,
                ParameterSelector,
                ParametersSelector,
                SourceLocationSelector,
                Selector);

            var markups = Enumerable.Repeat(new HtmlChunk(0, 0, string.Empty), 1)
                .Concat(from tokens in frames from token in tokens select token)
                .Pairwise((prev, curr) => new { Previous = prev, Current = curr })
                .SelectMany(
                    token => new[]
                    {
                        text.Substring(token.Previous.End, token.Current.Index - token.Previous.End),
                        token.Current.Html
                    }, (token, m) => m)
                .Where(m => m.Length > 0);

            var sb = new StringBuilder();
            foreach (var markup in markups)
            {
                sb.Append(markup);
            }

            return (sb.ToString(), list);
        }

        private static (HtmlChunk List, (HtmlChunk Type, HtmlChunk Name)[] Parameters) ParametersSelector(HtmlChunk p,
            IEnumerable<(HtmlChunk Type, HtmlChunk Name)> ps)
        {
            return (List: p, Parameters: ps.ToArray());
        }

        private static (HtmlChunk Type, HtmlChunk Name) ParameterSelector(HtmlChunk type, HtmlChunk name)
        {
            return (new HtmlChunk(type.Index, type.End, $"<span class='st-param-type'>{type.Html}</span>"),
                new HtmlChunk(name.Index, name.End, $"<span class='st-param-name'>{name.Html}</span>"));
        }

        private static ( HtmlChunk Type, HtmlChunk Method) MethodSelector(HtmlChunk type, HtmlChunk method)
        {
            return (new HtmlChunk(type.Index, type.End, $"<span class='st-type'>{type.Html}</span>"),
                new HtmlChunk(method.Index, method.End, $"<span class='st-method'>{method.Html}</span>"));
        }

        private static HtmlChunk TokenSelector(int idx, int len, string txt)
        {
            return new HtmlChunk(idx, idx + len, txt.Length > 0
                ? WebUtility.HtmlEncode(txt)
                : string.Empty);
        }
    }

    public struct HtmlChunk
    {
        public HtmlChunk(int index, int end, string html)
        {
            Index = index;
            End = end;
            Html = html;
        }

        public int Index { get; set; }

        public int End { get; set; }

        public string Html { get; set; }
    }
}