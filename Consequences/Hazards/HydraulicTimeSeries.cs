namespace Consequences.Hazards;

public class HydraulicTimeSeries:IHydraulicTimeseriesHazard
{

    // Hydraulic Time Series Data

    /// <summary>
    /// Array of times for depth and velocity in minutes from hydraulic start.
    /// </summary>
    public float[] TimeMinutes { get; }

    /// <summary>
    /// Array of depths at various points in time from hydraulic start.
    /// </summary>
    public float[] Depths { get; }

    /// <summary>
    /// Array of velocities at various points in time from hydraulic start.
    /// </summary>
    public float[] Velocities { get; }

    // Hydraulic Summary Data

    /// <summary>
    /// Maximum depth in time series.
    /// </summary>
    public float MaxDepth { get; }

    /// <summary>
    /// Maximum velocity in time series.
    /// </summary>
    public float MaxVelocity { get; }

    /// <summary>
    /// Maximum depth times velocity in time series.
    /// </summary>
    public float MaxDepthTimesVelocity { get; }

    /// <summary>
    /// Maximum depth times velocity squared in time series.
    /// </summary>
    public float MaxDepthTimesVelocitySquared { get; }

    public float Velocity => MaxVelocity;

    public float Depth => MaxDepth;

    /// <summary>
    /// This class takes a time series of depth and velocity data and simplifies it to remove redundant data points.
    /// </summary>
    /// <param name="allTimeValuesMinutes">Array of time values for depth and velocity.</param>
    /// <param name="allDepthValues">Array of depth values in time series.</param>
    /// <param name="allVelocityValues">Array of velocity values in time series.</param>
    /// <param name="pointReductionTolerance">Douglas-Peucker-Ramer reduction tolerance for removing reduntant data points. The higher the tolerance the more data will be removed.</param>
    public HydraulicTimeSeries(float[] allTimeValuesMinutes, float[] allDepthValues, float[] allVelocityValues, float pointReductionTolerance)
    {
        if (pointReductionTolerance > 0)
        {
            List<int> indicesToKeep = DouglasPeuckerReduction(allTimeValuesMinutes, allDepthValues, pointReductionTolerance);
            TimeMinutes = new float[indicesToKeep.Count];
            Depths = new float[indicesToKeep.Count];
            Velocities = new float[indicesToKeep.Count];
            for (int i = 0; i < indicesToKeep.Count; i++)
            {
                TimeMinutes[i] = allTimeValuesMinutes[indicesToKeep[i]];
                Depths[i] = allDepthValues[indicesToKeep[i]];
                Velocities[i] = allVelocityValues[indicesToKeep[i]];
            }

            for (int i = 0; i < allDepthValues.Length; i++)
            {
                if (allDepthValues[i] > MaxDepth) { MaxDepth = allDepthValues[i]; }
                if (allVelocityValues[i] > MaxVelocity) { MaxVelocity = allVelocityValues[i]; }
                if (allDepthValues[i] * allVelocityValues[i] > MaxDepthTimesVelocity) { MaxDepthTimesVelocity = allDepthValues[i] * allVelocityValues[i]; }
                if (allDepthValues[i] * (Math.Pow(allVelocityValues[i], 2)) > MaxDepthTimesVelocitySquared) { MaxDepthTimesVelocitySquared = (float)(allDepthValues[i] * (Math.Pow(allVelocityValues[i], 2))); }
            }
        }
        else
        {
            TimeMinutes = new float[allDepthValues.Length];
            Depths = new float[allDepthValues.Length];
            Velocities = new float[allDepthValues.Length];
            for (int i = 0; i < allDepthValues.Length; i++)
            {
                TimeMinutes[i] = allTimeValuesMinutes[i];
                Depths[i] = allDepthValues[i];
                Velocities[i] = allVelocityValues[i];

                if (allDepthValues[i] > MaxDepth) { MaxDepth = allDepthValues[i]; }
                if (allVelocityValues[i] > MaxVelocity) { MaxVelocity = allVelocityValues[i]; }
                if (allDepthValues[i] * allVelocityValues[i] > MaxDepthTimesVelocity) { MaxDepthTimesVelocity = allDepthValues[i] * allVelocityValues[i]; }
                if (allDepthValues[i] * (Math.Pow(allVelocityValues[i], 2)) > MaxDepthTimesVelocitySquared) { MaxDepthTimesVelocitySquared = (float)(allDepthValues[i] * (Math.Pow(allVelocityValues[i], 2))); }
            }

        }

    }

    /// <summary>
    /// This class takes a time series of depth and velocity data and simplifies it to remove redundant data points.
    /// </summary>
    /// <param name="storedBytes">Depth and velocity time series data stored as a byte array using the ToByteArray() routine.</param>
    public HydraulicTimeSeries(byte[] storedBytes)
    {
        using (MemoryStream ms = new MemoryStream(storedBytes))
        {
            using (BinaryReader br = new BinaryReader(ms))
            {
                int nValues = br.ReadInt32();
                MaxDepth = br.ReadSingle();
                MaxVelocity = br.ReadSingle();
                MaxDepthTimesVelocity = br.ReadSingle();
                MaxDepthTimesVelocitySquared = br.ReadSingle();
                TimeMinutes = new float[nValues];
                Depths = new float[nValues];
                Velocities = new float[nValues];
                for (int i = 0; i < nValues; i++)
                {
                    TimeMinutes[i] = br.ReadSingle();
                    Depths[i] = br.ReadSingle();
                    Velocities[i] = br.ReadSingle();
                }
            }
        }
    }

    /// <summary>
    /// Get summary time series for use in thread safe compute, advantages of compute safe version are time savings when sampling values in time varying compute.
    /// </summary>
    /// <returns></returns>
    public HydraulicTimeSeriesCompute GetComputer()
    {
        return new HydraulicTimeSeriesCompute(this);
    }

    /// <summary>
    /// Returns a list of indices to keep for the simplified x and y values of a line.
    /// OG Source: http://www.codeproject.com/Articles/18936/A-Csharp-Implementation-of-Douglas-Peucker-Line-Ap
    /// Modified to work with float arrays.
    /// </summary>
    /// <param name="xValues">x-values of the line.</param>
    /// <param name="yValues">y-values of the line.</param>
    /// <param name="tolerance">Tolerance to remove points. Higher tolerance will remove more points.</param>
    /// <returns></returns>
    private List<int> DouglasPeuckerReduction(float[] xValues, float[] yValues, float tolerance)
    {
        // Validate inputs
        var resultIndexes = new List<int>();
        if (xValues == null || yValues == null) { return resultIndexes; }
        if (xValues.Length != yValues.Length) { return resultIndexes; }
        if (xValues.Length < 3 || tolerance <= 0)
        {
            for (int i = 0; i < xValues.Length; i++) { resultIndexes.Add(i); }
            return resultIndexes;
        }

        int firstPoint = 0;
        int lastPoint = xValues.Length - 1;
        resultIndexes.Add(firstPoint);
        resultIndexes.Add(lastPoint);

        DouglasPeuckerReduction(firstPoint, lastPoint, xValues, yValues, tolerance, ref resultIndexes);

        resultIndexes.Sort();
        return resultIndexes;
    }

    private void DouglasPeuckerReduction(int firstPoint, int lastPoint, float[] xValues, float[] yValues, float tolerance, ref List<int> pointIndexesToKeep)
    {
        // No intermediate points
        if (lastPoint - firstPoint < 2) { return; }

        // Use an explicit stack to avoid recursive calls and reduce overhead
        var segments = new Stack<(int Start, int End)>();
        segments.Push((firstPoint, lastPoint));

        while (segments.Count > 0)
        {
            var (start, end) = segments.Pop();

            // If there are no points between start and end, continue
            if (end - start < 2) { continue; }

            float maxDistance = 0f;
            int indexFarthest = -1;

            // Search only intermediate points between start and end
            for (int index = start + 1; index < end; index++)
            {
                float distance = PerpendicularDistance(xValues[start], yValues[start], xValues[end], yValues[end], xValues[index], yValues[index]);
                if (distance > maxDistance)
                {
                    maxDistance = distance;
                    indexFarthest = index;
                }
            }

            // If the farthest point exceeds tolerance, keep it and process subsegments
            if (indexFarthest >= 0 && maxDistance > tolerance)
            {
                pointIndexesToKeep.Add(indexFarthest);

                // Push subsegments onto the stack
                segments.Push((start, indexFarthest));
                segments.Push((indexFarthest, end));
            }
        }
    }

    private float PerpendicularDistance(float aX, float aY, float bX, float bY, float cX, float cY)
    {
        // Area = |(1/2)(x1y2 + x2y3 + x3y1 - x2y1 - x3y2 - x1y3)|   *Area of triangle
        // Base = v((x1-x2)²+(x1-x2)²)                               *Base of Triangle*
        // Area = .5*Base*H                                          *Solve for height
        // Height = Area/.5/Base
        float area = Math.Abs((aX * bY + bX * cY + cX * aY - bX * aY - cX * bY - aX * cY) * 0.5f);
        float xDiff = (float)(aX - bX);
        float yDiff = (float)(aY - bY);
        float triangleBase; //= Math.Pow(xDiff * xDiff + yDiff * yDiff, 0.5);
        float num = xDiff * xDiff + yDiff * yDiff;

        // unsafe code to quickly estimate the square root.
        unsafe
        {
            float x2 = num * 0.5f;
            float y = num;
            uint i = *(uint*)&y;

            i = 0x5f3759df - (i >> 1);
            y = *(float*)&i;
            y = y * (1.5f - (x2 * y * y));

            triangleBase = 1.0f / y;
        }

        //triangleBase= (float)Math.Pow(xDiff * xDiff + yDiff * yDiff, 0.5);


        return (area * 2) / triangleBase;
    }

    /// <summary>
    /// Serialize the summary hydraulic time series to a byte array.
    /// </summary>
    /// <returns>serialized byte array of hydraulic time series.</returns>
    public byte[] ToByteArray()
    {
        List<byte> b = new List<byte>();

        b.AddRange(BitConverter.GetBytes(Depths.Length));
        b.AddRange(BitConverter.GetBytes(MaxDepth));
        b.AddRange(BitConverter.GetBytes(MaxVelocity));
        b.AddRange(BitConverter.GetBytes(MaxDepthTimesVelocity));
        b.AddRange(BitConverter.GetBytes(MaxDepthTimesVelocitySquared));
        for (int i = 0; i < Depths.Length; i++)
        {
            b.AddRange(BitConverter.GetBytes(TimeMinutes[i]));
            b.AddRange(BitConverter.GetBytes(Depths[i]));
            b.AddRange(BitConverter.GetBytes(Velocities[i]));
        }
        // 
        return b.ToArray();
    }


    /// <summary>
    /// Get the depth value for a given point in time. depth will be linearly interpolated if time falls between two points.
    /// </summary>
    /// <param name="timeRelativeToHydraulicStart">Time in minutes relative to the start to sample depth.</param>
    /// <returns></returns>
    public float GetDepth(float timeRelativeToHydraulicStart)
    {
        if (timeRelativeToHydraulicStart < TimeMinutes[0]) { return 0; }
        else if (timeRelativeToHydraulicStart >= TimeMinutes[TimeMinutes.Count() - 1]) { return Depths[Depths.Count() - 1]; }
        else
        {
            // '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
            // Update the position in the time series and the slopes if the position has changed
            // '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
            var timeIndex = Array.BinarySearch(TimeMinutes, (Single)timeRelativeToHydraulicStart);
            if (timeIndex < 0) { timeIndex = -1 * timeIndex - 1; }
            // 
            var currentDepthSlope = (Depths[timeIndex] - Depths[timeIndex - 1]) / (float)(TimeMinutes[timeIndex] - TimeMinutes[timeIndex - 1]);
            // '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
            // Calculate the depth and velocity using linear interpolation
            // y = y0+((y1-y0)/(x1-x0))*(x-x0) = y0 + slope*(x-x0) simple linear interpolation
            // simplified further to y = f+slope*x where f = y0+(slope*-x0)
            // '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
            return (float)(Depths[timeIndex - 1] + currentDepthSlope * (timeRelativeToHydraulicStart - TimeMinutes[timeIndex - 1]));
        }
    }

    /// <summary>
    /// Get the first occurrence in time where a given depth value is exceeded.
    /// </summary>
    /// <param name="depthValue">The depth value to search for exceedance.</param>
    /// <returns></returns>
    public float GetMinutesToDepth(float depthValue)
    {
        if (depthValue > MaxDepth) { return -1; }
        if (Depths[0] > depthValue) { return 0; }
        // 
        for (int i = 0; i < Depths.Length; i++)
        {
            if (Depths[i] > depthValue)
            {
                return (float)(TimeMinutes[i - 1] + ((depthValue - Depths[i - 1]) / (Depths[i] - Depths[i - 1])) * (TimeMinutes[i] - TimeMinutes[i - 1]));
            }
        }
        // 
        return -1;
    }

    /// <summary>
    /// Get the first occurrence in time where a given depth value is exceeded.
    /// </summary>
    /// <param name="depthThreshold">The depth value to search for exceedance.</param>
    /// <returns></returns>
    public float GetDurationMinutes(float depthThreshold)
    {
        if (depthThreshold > MaxDepth) { return 0; }

        // Count duration above depth threshold
        float totalDuration = 0;
        float interpTime;
        for (int i = 0; i < Depths.Length - 1; i++)
        {
            if ((Depths[i] < depthThreshold) && Depths[i + 1] < depthThreshold) { continue; } // Both Below
            else if ((Depths[i] >= depthThreshold) && Depths[i + 1] >= depthThreshold) { totalDuration += TimeMinutes[i + 1] - TimeMinutes[i]; } // Both Above
            else
            {
                interpTime = (TimeMinutes[i] + ((depthThreshold - Depths[i]) / (Depths[i + 1] - Depths[i])) * (TimeMinutes[i + 1] - TimeMinutes[i]));
                if (Depths[i] < depthThreshold) // Slope must be increasing
                {
                    totalDuration += TimeMinutes[i + 1] - interpTime;
                }
                else // Slope must be decreasing    
                {
                    totalDuration += interpTime - TimeMinutes[i];
                }
            }
        }
        // 
        return totalDuration;
    }

    /// <summary>
    /// Class to more efficiently transverse hydraulic time series data when the time checks increase
    /// </summary>
    public class HydraulicTimeSeriesCompute
    {
        private HydraulicTimeSeries _parentTimeSeries;
        private int _currentIndex = 0;
        private int _currentNextIndex = 1;
        private float _currentDepthSlope;
        private float _currentVelocitySlope;

        private float _currentTime = float.MinValue;
        private float _currentDepth;
        private float _currentVelocity;
        private float _currentDV;
        private float _currentDVsquared;

        public HydraulicTimeSeriesCompute(HydraulicTimeSeries parentSeries)
        {
            _parentTimeSeries = parentSeries;
        }

        public float GetCurrentDepthAndVelocity(float timeRelativeToHydraulicStart, ref float velocity, ref float DV, ref float DVsquared)
        {
            // '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
            // Early exit strategies
            // '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
            if (_currentTime == timeRelativeToHydraulicStart)
            {
                velocity = _currentVelocity;
                DV = _currentDV;
                DVsquared = _currentDVsquared;
                return _currentDepth;
            }


            _currentTime = timeRelativeToHydraulicStart;
            if (_currentTime < _parentTimeSeries.TimeMinutes[0])
            {
                _currentIndex = 0;
                _currentNextIndex = 1;
                _currentDepth = 0;
                _currentVelocity = 0;
                _currentDV = 0;
                _currentDVsquared = 0;
            }
            else if (_currentTime >= _parentTimeSeries.TimeMinutes.Last())
            {
                _currentIndex = _parentTimeSeries.TimeMinutes.Length - 2;
                _currentNextIndex = _parentTimeSeries.TimeMinutes.Length - 1;
                _currentDepth = _parentTimeSeries.Depths[_currentNextIndex];
                _currentVelocity = _parentTimeSeries.Velocities[_currentNextIndex];
                _currentDV = _currentDepth * _currentVelocity;
                _currentDVsquared = _currentDepth * _currentVelocity * _currentVelocity;
            }
            else
            {
                // '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
                // Update the position in the time series and the slopes if the position has changed
                // '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
                if (_currentTime > _parentTimeSeries.TimeMinutes[_currentNextIndex] || _currentTime < _parentTimeSeries.TimeMinutes[_currentIndex])
                {
                    // If _parentTimeSeries._timeMinutes(_currentNextIndex + 1) < timeRelativeToHydraulicStart Then 'first check next option for early exit when running in sequence
                    // _currentIndex += 1
                    // _currentNextIndex += 1
                    // Else 'find it the hard way
                    _currentNextIndex = Array.BinarySearch(_parentTimeSeries.TimeMinutes, (float)_currentTime);
                    if (_currentNextIndex < 0)
                    {
                        _currentNextIndex = -1 * _currentNextIndex - 1;
                        _currentIndex = _currentNextIndex - 1;
                    }
                    // End If
                    // 
                    _currentDepthSlope = (_parentTimeSeries.Depths[_currentNextIndex] - _parentTimeSeries.Depths[_currentNextIndex - 1]) / (float)(_parentTimeSeries.TimeMinutes[_currentNextIndex] - _parentTimeSeries.TimeMinutes[_currentNextIndex - 1]);
                    _currentVelocitySlope = (_parentTimeSeries.Velocities[_currentNextIndex] - _parentTimeSeries.Velocities[_currentNextIndex - 1]) / (float)(_parentTimeSeries.TimeMinutes[_currentNextIndex] - _parentTimeSeries.TimeMinutes[_currentNextIndex - 1]);
                }
                // '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
                // Calculate the depth and velocity using linear interpolation
                // y = y0+((y1-y0)/(x1-x0))*(x-x0) = y0 + slope*(x-x0) simple linear interpolation
                // simplified further to y = f+slope*x where f = y0+(slope*-x0)
                // '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
                _currentDepth = (float)(_parentTimeSeries.Depths[_currentNextIndex - 1] + _currentDepthSlope * (_currentTime - _parentTimeSeries.TimeMinutes[_currentNextIndex - 1]));
                _currentVelocity = (float)(_parentTimeSeries.Velocities[_currentNextIndex - 1] + _currentVelocitySlope * (_currentTime - _parentTimeSeries.TimeMinutes[_currentNextIndex - 1]));
                _currentDV = _currentDepth * _currentVelocity;
                _currentDVsquared = _currentDepth * _currentVelocity * _currentVelocity;
            }
            // 
            velocity = _currentVelocity;
            DV = _currentDV;
            DVsquared = _currentDVsquared;
            return _currentDepth;
        }
    }
}
