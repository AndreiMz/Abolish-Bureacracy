﻿using System;
using System.Collections.Generic;
using System.Linq;
using static FormReader.Structures;
using FormReader.Debugger;

namespace FormReader
{
    public class FormReader
    {
        private FormReaderOptions Options;
        private ImagePreprocessor Preprocessor;
        // this here cause annoying compielr warning
        private Painter Painter = null;

        private Logger Logger;
        public FormReader(FormReaderOptions options)
        {
            this.Options = options;
            this.Preprocessor = new ImagePreprocessor(options.InputFile);
            // Debug image.
            this.Painter = options.GenerateDebugImage ? new Painter(this.Preprocessor.imgHeight, this.Preprocessor.imgWidth) : null;
            this.Logger = options.Verbose ? new Logger(options) : null;
        }

        public FormReaderResult Process()
        {
            var boxes = this.FindBoxes();

            if (Options.Verbose)
            {
                this.Logger.LogResult(boxes);
            }
            

            FormReaderResult result = new FormReaderResult();
            result.Boxes = boxes;
            result.ImageHexa = this.Preprocessor.imgHexa;

            return result;

        }

        /// <summary>
        /// We are seaching for pattern!
        /// We look for junctions.
        /// This will help us make a decision.
        /// Junction types: T, L, +.
        /// Junctions allow us to find boxes contours.
        /// </summary>
        /// <returns></returns>
        private CachedJunctions FindJunctions()
        {

            int minJunctionWidth = this.Options.JunctionWidth;
            int minJunctionHeight = this.Options.JunctionHeight;

            // Cache per line speed up the creation of various cache.
            Dictionary<int, List<Junction>> cacheListJunctionPerLine = new Dictionary<int, List<Junction>>();
            List<Junction> listJunction = new List<Junction>();

            for (int y = 1; y < this.Preprocessor.imgHeight - 1; y++)
            {
                List<Junction> listJunctionX = null;
                int proximityCounter = 0;

                for (int x = 1; x < this.Preprocessor.imgWidth - 1; x++)
                {
                    Junction? junction = GetJunction(this.Preprocessor.imgData, this.Preprocessor.imgHeight, this.Preprocessor.imgWidth, minJunctionHeight, minJunctionWidth, y, x);
                    if (junction != null)
                    {
                        if (listJunctionX == null)
                        {
                            listJunctionX = new List<Junction>();
                        }
                        listJunctionX.Add(junction.Value);
                        proximityCounter++;
                    }
                    else
                    {
                        if (listJunctionX != null)
                        {
                            if (proximityCounter < this.Options.maxProximity)
                            {
                                if (!cacheListJunctionPerLine.ContainsKey(y))
                                {
                                    cacheListJunctionPerLine.Add(y, new List<Junction>());
                                }
                                cacheListJunctionPerLine[y].AddRange(listJunctionX);
                                listJunction.AddRange(listJunctionX);
                                listJunctionX.Clear();
                            }
                            else
                            {
                                listJunctionX.Clear();
                            }
                        }
                        proximityCounter = 0;
                    }
                }

                if (proximityCounter < this.Options.maxProximity && listJunctionX != null)
                {
                    if (!cacheListJunctionPerLine.ContainsKey(y))
                    {
                        cacheListJunctionPerLine.Add(y, new List<Junction>());
                    }
                    cacheListJunctionPerLine[y].AddRange(listJunctionX);
                    listJunction.AddRange(listJunctionX);
                }

            }

            if (listJunction.Count >= Options.MaxJunctions)
            {
                // Something wrong happen. Too much junction for now.
                // If we continue, we would spend too much time processing the image.
                // Let's suppose we don't know.
                // Too many junctions. The image seem too complex. You may want to increase MaxJunctions
                throw (new Exception("Too many Junctions"));
            }

            CachedJunctions junctions;

            junctions.cacheListJunctionPerLine = cacheListJunctionPerLine;
            junctions.listJunction = listJunction;

            if (this.Options.Verbose)
            {
                Logger.LogGenericMessage("Junction.count: " + listJunction.Count);
            }

            return junctions;
        }

        private CachedJunctionCombinations GetCombinedJunctions(List<Junction> listJunction, Dictionary<int, List<Junction>> cacheListJunctionPerLine)
        {
            // Let's check the list of points.

            // Prepare cache to speedup searching algo.
            Dictionary<int, Junction[]> cacheNearJunction = new Dictionary<int, Junction[]>();
            Dictionary<int, Junction[]> cachePossibleNextJunctionRight = new Dictionary<int, Junction[]>();
            Dictionary<int, Junction[]> cachePossibleNextJunctionLeft = new Dictionary<int, Junction[]>();
            foreach (var junction in listJunction)
            {
                var listJunctionNearJunction = new List<Junction>();

                for (int deltaY = -this.Options.variationY; deltaY <= this.Options.variationY; deltaY++)
                {
                    if (cacheListJunctionPerLine.ContainsKey(junction.Y - deltaY))
                        listJunctionNearJunction.AddRange(cacheListJunctionPerLine[junction.Y - deltaY]);
                }

                var list = listJunctionNearJunction
                    .Where(m =>
                        Math.Abs(m.X - junction.X) <= this.Options.maxX
                        )
                    .ToArray();

                var id = BuildDictionaryId(junction.X, junction.Y);

                cacheNearJunction.Add(id, list);

                var possibleNextJunction = list
                    .Where(m =>
                        Math.Abs(m.X - junction.X) >= this.Options.minX
                        )
                    .ToList();

                cachePossibleNextJunctionLeft.Add(id, possibleNextJunction.Where(m => m.X < junction.X).ToArray());
                cachePossibleNextJunctionRight.Add(id, possibleNextJunction.Where(m => m.X > junction.X).ToArray());
            }

            CachedJunctionCombinations combinations;
            combinations.cacheNearJunction = cacheNearJunction;
            combinations.cachePossibleNextJunctionLeft = cachePossibleNextJunctionLeft;
            combinations.cachePossibleNextJunctionRight = cachePossibleNextJunctionRight;

            return combinations;
        }

        private List<Line> GetLines(List<Junction> listJunction, CachedJunctionCombinations junctionCombinations)
        {
            // Let's check the list of points.

            int numSol = 0;

            List<Line> possibleSol = new List<Line>();

            // We use a dictionary here because we need a fast way to remove entry.
            // We reduce computation and we also merge solutions.
            var elements = listJunction.OrderBy(m => m.Y).ToDictionary(m => BuildDictionaryId(m.X, m.Y ), m => m);

            int skipSol = 0;
            while (elements.Any())
            {
                var start = elements.First().Value;
                elements.Remove(BuildDictionaryId(start.X, start.Y));

                Dictionary<int, List<int>> usedJunctionsForGapX = new Dictionary<int, List<int>>();
                List<Line> listSolutions = new List<Line>();
                var junctionsForGap = junctionCombinations.cacheNearJunction[BuildDictionaryId(start.X, start.Y)];

                for (int iGap = 0; iGap < junctionsForGap.Length; iGap++)
                {
                    var gap = junctionsForGap[iGap];

                    // Useless because it's already done with: cacheNearJunction.
                    /*
					var gapY = Math.Abs(gap.Y - start.Y);
					if (gapY > 2)
					{
						continue;
					}*/

                    var gapX = Math.Abs(gap.X - start.X);
                    if (gapX <= this.Options.minX || gapX > this.Options.maxX)
                    {
                        continue;
                    }

                    // We will reduce list of solution by checking if the solution is already found.
                    //if (listSolutions.Any(m => Math.Abs(m.GapX - gapX) < 2 && m.Junctions.Contains(start)))
                    if (usedJunctionsForGapX.ContainsKey(BuildDictionaryId(gap.X, gap.Y)) &&
                        usedJunctionsForGapX[BuildDictionaryId(gap.X, gap.Y)].Any(m => Math.Abs(m - gapX) < 10))
                    {
                        skipSol++;
                        continue;
                    }

                    List<Junction> curSolution = new List<Junction>();
                    curSolution.Add(start);

                    int numElementsRight = FindElementsOnDirection(junctionCombinations.cachePossibleNextJunctionRight, start, gapX, curSolution);
                    int numElementsLeft = FindElementsOnDirection(junctionCombinations.cachePossibleNextJunctionLeft, start, -gapX, curSolution);

                    int numElements = numElementsLeft + numElementsRight;

                    if (numElements >= this.Options.MinNumElements)
                    {
                        if (numSol == this.Options.MaxSolutions)
                        {
                            // Something wrong happen. Too much solution for now.
                            // If we continue, we would spend too much time processing the image.
                            // Let's suppose we don't know.
                            throw (new Exception("Too much solution somehow"));
                            /*
                            return new OpenFieldReaderResult
                            {
                                // Too much solution. You may want to increase MaxSolutions.
                                ReturnCode = 30
                            };
                            */
                        }

                        numSol++;
                        listSolutions.Add(new Line
                        {
                            GapX = gapX,
                            Junctions = curSolution.ToArray()
                        });
                        foreach (var item in curSolution)
                        {
                            List<int> listGapX;
                            if (!usedJunctionsForGapX.ContainsKey(BuildDictionaryId(item.X, item.Y)))
                            {
                                listGapX = new List<int>();
                                usedJunctionsForGapX.Add(BuildDictionaryId(item.X, item.Y), listGapX);
                            }
                            else
                            {
                                listGapX = usedJunctionsForGapX[BuildDictionaryId(item.X, item.Y)];
                            }
                            listGapX.Add(gapX);
                        }
                    }
                }

                Line bestSol = listSolutions.OrderByDescending(m => m.Junctions.Count()).FirstOrDefault();

                if (bestSol != null)
                {
                    // Too slow. (faster if we skip removal)
                    // But, we have more solutions.
                    foreach (var item in bestSol.Junctions)
                    {
                        elements.Remove(BuildDictionaryId(item.X, item.Y));
                    }

                    possibleSol.Add(bestSol);
                }
            }

            if (this.Options.Verbose)
            {
                this.Logger.LogSolutions(numSol, possibleSol, skipSol);
            }

            return possibleSol;
        }


        private List<List<Box>> FindBoxes()
        {


            // Cache per line speed up the creation of various cache.
            CachedJunctions cachedJunctions = this.FindJunctions();


            var cachedJunctionCombinations = GetCombinedJunctions(cachedJunctions.listJunction, cachedJunctions.cacheListJunctionPerLine);

            var possibleSol = GetLines(cachedJunctions.listJunction, cachedJunctionCombinations);

            List<LineCluster> lineClusters = ClusterLines(cachedJunctionCombinations, possibleSol);

            List<BoxesCluster> boxesClusters = PrototypeBoxClusters(lineClusters);

            List<List<Box>> allBoxes = GetBoxes(boxesClusters);

            RemoveInvalidBoxes(allBoxes);

            if (this.Options.GenerateDebugImage)
            {
                this.Painter.DrawImage();
            }

            return allBoxes;
        }

        private List<BoxesCluster> PrototypeBoxClusters(List<LineCluster> lineClusters)
        {
            List<BoxesCluster> boxesClusters = new List<BoxesCluster>();
            Dictionary<LineCluster, LineCluster> lineClustersTop = new Dictionary<LineCluster, LineCluster>();
            Dictionary<LineCluster, LineCluster> lineClustersBottom = new Dictionary<LineCluster, LineCluster>();

            Dictionary<LineCluster, float> cacheGapX = new Dictionary<LineCluster, float>();

            // Merge top and bottom lines.
            // I could optimize this
            foreach (var itemA in lineClusters)
            {
                foreach (var itemB in lineClusters)
                {
                    if (itemA != itemB)
                    {
                        if (itemA.Bottom && itemB.Top || itemA.Top && itemB.Bottom)
                        {
                            // Compatible.
                            var topLine = itemA.Top ? itemA : itemB;
                            var bottomLine = itemA.Top ? itemB : itemA;

                            if (lineClustersTop.ContainsKey(topLine) || lineClustersBottom.ContainsKey(bottomLine))
                                continue;

                            if (!cacheGapX.ContainsKey(itemA))
                                cacheGapX.Add(itemA, itemA.Junctions.Average(m => m.GapX));
                            if (!cacheGapX.ContainsKey(itemB))
                                cacheGapX.Add(itemB, itemB.Junctions.Average(m => m.GapX));

                            var firstGapX = cacheGapX[itemA];
                            var secondGapX = cacheGapX[itemB];

                            // GapX should be similar. Otherwise, just ignore it.
                            // TODO: I don't even know how to start fixing THIS filthy hardcoding
                            if (Math.Abs(firstGapX - secondGapX) <= 2 && Math.Abs(itemA.X - itemB.X) < 200)
                            {
                                var avgGapX = (firstGapX + secondGapX) / 2;

                                var minGapY = Math.Max(10, avgGapX - 5);
                                var maxGapY = Math.Min(this.Options.maxX, avgGapX + 5);

                                int diffY = topLine.Y - bottomLine.Y;
                                if (diffY >= maxGapY && diffY < minGapY)
                                {
                                    continue;
                                }

                                // For the majority of element on top line, we should be able to interconnect
                                // with the other line.

                                // Must have some common element next to each other.
                                int commonElementCounter = 0;

                                int groupGapX = Math.Max(5, (int)avgGapX - 5);

                                int numberOfElements = 5;

                                // We reduce required computation.
                                // We will consider only some elements on the junctions.
                                List<Junction> topLineJunctions = topLine.Junctions.GroupBy(m => (m.X / groupGapX))
                                    .SelectMany(m => m.Take(numberOfElements))
                                    .ToList();
                                List<Junction> bottomLineJunctions = bottomLine.Junctions.GroupBy(m => (m.X / groupGapX))
                                    .SelectMany(m => m.Take(numberOfElements))
                                    .ToList();

                                int minPercent = 70;
                                int minCount = Math.Min(topLineJunctions.Count, bottomLineJunctions.Count);
                                int minimumCommonElements = minCount * minPercent / 100;

                                List<float> avgGapY = new List<float>();
                                foreach (var topJunction in topLineJunctions)
                                {
                                    var commonElement = bottomLineJunctions.Where(m =>
                                        Math.Abs(topJunction.X - m.X) <= 5
                                        && topJunction.Y - m.Y >= minGapY
                                        && topJunction.Y - m.Y <= maxGapY
                                    );
                                    if (commonElement.Any())
                                    {
                                        avgGapY.Add((float)commonElement.Average(m => topJunction.Y - m.Y));
                                        commonElementCounter++;

                                        if (commonElementCounter >= minimumCommonElements)
                                        {
                                            // We can stop now. It's a boxes!
                                            break;
                                        }
                                    }
                                }

                                if (commonElementCounter >= 1 && commonElementCounter >= minimumCommonElements)
                                {
                                    boxesClusters.Add(new BoxesCluster
                                    {
                                        TopLine = topLine,
                                        BottomLine = bottomLine,
                                        GapY = avgGapY.Average()
                                    });

                                    lineClustersTop.Add(topLine, topLine);
                                    lineClustersBottom.Add(bottomLine, bottomLine);
                                }
                            }
                        }
                    }
                }
            }

            if (this.Options.GenerateDebugImage)
            {
                this.Painter.DrawJunctions(lineClusters, boxesClusters);
            }

            return boxesClusters;
        }


        /// <summary>
        // We can now merge near junctions.
        // We want to find the centroid in order to determine the boxes dimensions and position.
        /// </summary>
        /// <param name="boxesClusters"></param>
        /// <returns></returns>
        private List<List<Box>> GetBoxes(List<BoxesCluster> boxesClusters)
        {

            List<List<Box>> allBoxes = new List<List<Box>>();
            foreach (var boxesCluster in boxesClusters)
            {
                // We will explore points horizontally.
                var allPoints = boxesCluster.TopLine.Junctions.Union(boxesCluster.BottomLine.Junctions)
                    .Select(m => m.X)
                    .OrderBy(m => m)
                    .ToList();

                var avgGapX = boxesCluster.TopLine.Junctions.Average(m => m.GapX);
                var maxDist = Math.Max(10, avgGapX / 2);

                // Sometime, there is missing points. We will interconnect the boxes.
                List<Box> boxes = new List<Box>();
                Box curBoxes = null;

                while (allPoints.Any())
                {
                    var listX = new List<int>();

                    var x = allPoints[0];
                    allPoints.RemoveAt(0);
                    listX.Add(x);

                    // Remove near points.
                    for (int i = 0; i < allPoints.Count; i++)
                    {
                        var curX = allPoints[i];
                        if (Math.Abs(curX - x) < maxDist)
                        {
                            allPoints.RemoveAt(i);
                            i--;
                            listX.Add(curX);
                        }
                    }

                    var centroidX = listX.Average();

                    var topJunctions = boxesCluster.TopLine.Junctions.Where(m => Math.Abs(m.X - centroidX) < maxDist);
                    var bottomJunctions = boxesCluster.BottomLine.Junctions.Where(m => Math.Abs(m.X - centroidX) < maxDist);

                    Point? curPointTop = null;
                    Point? curPointBottom = null;

                    if (bottomJunctions.Any())
                    {
                        var curX = bottomJunctions.Average(m => m.X);
                        var curY = bottomJunctions.Average(m => m.Y);
                        curPointTop = new Point(curX, curY);
                    }

                    if (topJunctions.Any())
                    {
                        var curX = topJunctions.Average(m => m.X);
                        var curY = topJunctions.Average(m => m.Y);
                        curPointBottom = new Point(curX, curY);
                    }

                    if (topJunctions.Any() != bottomJunctions.Any())
                    {
                        // We should try our best to correct the error.
                        // If we use boxesCluster.GapY we can estimate the point.

                        if (!curPointTop.HasValue)
                            curPointTop = new Point(curPointBottom.Value.X, curPointBottom.Value.Y - boxesCluster.GapY);

                        if (!curPointBottom.HasValue)
                            curPointBottom = new Point(curPointTop.Value.X, curPointTop.Value.Y + boxesCluster.GapY);
                    }

                    if (!curPointTop.HasValue && !curPointBottom.HasValue)
                    {
                        // This should not happen. Please open an issue on GitHub with your image.
                        throw (new Exception("This should not happen. BIG YIKES"));
                    }

                    if (curBoxes == null)
                    {
                        curBoxes = new Box();
                        curBoxes.TopLeft = curPointTop.Value;
                        curBoxes.BottomLeft = curPointBottom.Value;
                    }
                    else
                    {
                        curBoxes.TopRight = curPointTop.Value;
                        curBoxes.BottomRight = curPointBottom.Value;
                        boxes.Add(curBoxes);

                        // Prepare the next box. (may not be added)
                        curBoxes = new Box();
                        curBoxes.TopLeft = curPointTop.Value;
                        curBoxes.BottomLeft = curPointBottom.Value;
                    }
                }

                if (boxes.Any())
                {
                    allBoxes.Add(boxes);
                }
            }

            return allBoxes;
        }

        private List<LineCluster> ClusterLines(CachedJunctionCombinations cachedJunctionCombinations, List<Line> possibleSol)
        {
            Dictionary<int, Junction[]> junctionsPerGroup = VerticalGroupJunctions(cachedJunctionCombinations, possibleSol);

            List<LineCluster> lineClusters = HorizontalGroupLines(junctionsPerGroup);
            return lineClusters;
        }

        private Dictionary<int, Junction[]> VerticalGroupJunctions(CachedJunctionCombinations cachedJunctionCombinations, List<Line> possibleSol)
        {
            // Let's merge near junctions. (vertical line)
            // We assign a group id for each clusters.

            Dictionary<int, int> junctionToGroupId = new Dictionary<int, int>();

            int nextGroupId = 1;
            foreach (var curSolution in possibleSol)
            {
                if (curSolution.Junctions.First().GroupId == 0)
                {
                    for (int i = 0; i < curSolution.Junctions.Length; i++)
                    {
                        ref var j = ref curSolution.Junctions[i];
                        j.GapX = curSolution.GapX;
                    }

                    // Not assigned yet.

                    // Find near junction.
                    int groupId = 0;

                    foreach (var item in curSolution.Junctions)
                    {
                        var alreadyClassified = cachedJunctionCombinations.cacheNearJunction[BuildDictionaryId(item.X, item.Y)]
                            .Where(m =>
                                // Doesn't work with struct.
                                //m.GroupId != 0 &&
                                //TODO: More hardcoding
                                Math.Abs(m.X - item.X) <= 5 &&
                                Math.Abs(m.Y - item.Y) <= 3
                            // Doesn't work with struct.
                            //Math.Abs(m.GapX - item.GapX) <= 2
                            ).Where(m => junctionToGroupId.ContainsKey(BuildDictionaryId(m.X, m.Y)));
                        if (alreadyClassified.Any())
                        {
                            Junction junction = alreadyClassified.First();
                            groupId = junctionToGroupId[BuildDictionaryId(junction.X, junction.Y)];
                            //groupId = alreadyClassified.First().GroupId;
                            break;
                        }
                    }

                    if (groupId == 0)
                    {
                        // Not found.
                        // Create a new group.
                        nextGroupId++;

                        groupId = nextGroupId;
                    }

                    for (int i = 0; i < curSolution.Junctions.Length; i++)
                    {
                        ref var j = ref curSolution.Junctions[i];
                        j.GroupId = groupId;
                        int id = BuildDictionaryId(j.X, j.Y);
                        if (!junctionToGroupId.ContainsKey(id))
                        {
                            junctionToGroupId.Add(id, groupId);
                        }
                    }
                }
            }

            Dictionary<int, Junction[]> junctionsPerGroup = possibleSol
                .SelectMany(m => m.Junctions)
                .GroupBy(m => m.GroupId)
                .ToDictionary(m => m.Key, m => m.ToArray());
            return junctionsPerGroup;
        }

        /// <summary>
        /// Let's explore the clusters directions and try to interconnect clusters on the horizontal side.
        /// </summary>
        /// <param name="junctionsPerGroup"></param>
        /// <returns></returns>
        private List<LineCluster> HorizontalGroupLines(Dictionary<int, Junction[]> junctionsPerGroup)
        {
            
            List<LineCluster> lineClusters = new List<LineCluster>();

            foreach (var item in junctionsPerGroup)
            {
                int groupId = item.Key;
                Junction[] junctions = item.Value;

                int minElementDir = this.Options.minElementPercent * junctions.Length / 100;

                // Determine the general direction.
                var top = junctions.Count(m => m.Top) > minElementDir;
                var bottom = junctions.Count(m => m.Bottom) > minElementDir;

                for (int i = 0; i < junctions.Length; i++)
                {
                    ref var j = ref junctions[i];
                    j.Top = top;
                    j.Bottom = bottom;
                }

                var x = (int)junctions.Average(m => m.X);
                var y = (int)junctions.Average(m => m.Y);

                lineClusters.Add(new LineCluster
                {
                    Bottom = bottom,
                    Top = top,
                    Junctions = junctions,
                    X = x,
                    Y = y
                });
            }

            return lineClusters;
        }


        private void RemoveInvalidBoxes(List<List<Box>> allBoxes)
        {
            // Let's explore boxes!
            // We will check if those boxes seem valid.
            for (int i = 0; i < allBoxes.Count; i++)
            {
                var isValid = true;
                var curBoxes = allBoxes[i];

                var minWidth = curBoxes.Min(m =>
                    (m.GetWidth()));
                var minHeight = curBoxes.Min(m =>
                    (m.GetHeight()));
                var maxWidth = curBoxes.Max(m =>
                    (m.GetWidth()));
                var maxHeight = curBoxes.Max(m =>
                    (m.GetHeight()));

                // HARDCODED FILTH
                // If the width and height are too different, we should not consider the boxes.
                if (maxWidth - minWidth > 7 || maxHeight - minHeight > 5)
                {
                    isValid = false;
                }

                if (!isValid)
                {
                    allBoxes.RemoveAt(i);
                    i--;
                }
            }

            if (this.Options.GenerateDebugImage)
            {
                this.Painter.DrawBoxes(allBoxes);
            }
        }

        private static int FindElementsOnDirection(Dictionary<int, Junction[]> cacheNearJunction, Junction start, int gapX, List<Junction> curSolution)
        {
            int numElements = 0;
            var x = start.X;
            var y = start.Y;
            Junction[] remainingList = cacheNearJunction[BuildDictionaryId(start.X, start.Y)];

            // We prefer a distX of 0.

            for (int iNext = 0; iNext < remainingList.Length; iNext++)
            {
                var cur = remainingList[iNext];
                var curX = cur.X;
                var curY = cur.Y;

                int distX = Math.Abs(x + gapX - curX);

                // TODO: should be a parameter
                // If you use dilatation, it should be distX == 0. (faster)
                if (distX <= 0)
                {
                    numElements++;
                    curSolution.Add(cur);

                    remainingList = cacheNearJunction[BuildDictionaryId(cur.X, cur.Y)];
                    x = curX;
                    y = curY;

                    iNext = -1;
                    continue;
                }
            }

            // No element found or the end.
            return numElements;
        }

        private static Junction? GetJunction(int[] imgData, int row, int col, int height, int width, int y, int x)
        {
            var val = GetVal(imgData, y, x, row);
            if (0 < val)
            {
                // Let's explore the directions.

                byte numTop = 0;
                if (y - height >= 1)
                    for (int i = 0; i < height; i++)
                        if (GetVal(imgData, y - i, x, row) == val)
                            numTop++;
                        else
                            break;

                byte numBottom = 0;
                if (y + height < row - 1)
                    for (int i = 0; i < height; i++)
                        if (GetVal(imgData, y + i, x, row) == val)
                            numBottom++;
                        else
                            break;

                byte numRight = 0;
                if (x + width < col - 1)
                    for (int i = 0; i < width; i++)
                        if (GetVal(imgData, y, x + i, row) == val)
                            numRight++;
                        else
                            break;

                byte numLeft = 0;
                if (x - width >= 1)
                    for (int i = 0; i < width; i++)
                        if (GetVal(imgData, y, x - i, row) == val)
                            numLeft++;
                        else
                            break;

                var top = numTop >= height;
                var bottom = numBottom >= height;
                var left = numLeft >= width;
                var right = numRight >= width;

                if ((top || bottom) && (left || right))
                {
                    return new Junction
                    {
                        Bottom = bottom,
                        Left = left,
                        Right = right,
                        Top = top,
                        NumBottom = numBottom,
                        NumLeft = numLeft,
                        NumRight = numRight,
                        NumTop = numTop,
                        X = x,
                        Y = y
                    };
                }
            }
            return null;
        }

        private static int GetVal(int[] imgData, int y, int x, int row)
        {
            return (
                imgData[y + (x - 1) * row] |
                imgData[y + x * row] |
                imgData[y + (x + 1) * row]);
        }
    }
}
