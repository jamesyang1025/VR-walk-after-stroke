using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class CPC_Visual
{
    public Color pathColor = Color.green;
    public Color inactivePathColor = Color.gray;
    public Color frustrumColor = Color.white;
    public Color handleColor = Color.yellow;
}

public enum CPC_ECurveType
{
    EaseInAndOut,
    Linear,
    Custom
}

public enum CPC_EAfterLoop
{
    Continue,
    Stop
}

[System.Serializable]
public class CPC_Point
{

    public Vector3 position;
    public Quaternion rotation;
    public Vector3 handleprev;
    public Vector3 handlenext;
    public CPC_ECurveType curveTypeRotation;
    public AnimationCurve rotationCurve;
    public CPC_ECurveType curveTypePosition;
    public AnimationCurve positionCurve;
    public bool chained;

    public CPC_Point(Vector3 pos, Quaternion rot)
    {
        position = pos;
        rotation = rot;
        handleprev = Vector3.back;
        handlenext = Vector3.forward;
        curveTypeRotation = CPC_ECurveType.EaseInAndOut;
        rotationCurve = AnimationCurve.EaseInOut(0,0,1,1);
        curveTypePosition = CPC_ECurveType.Linear;
        positionCurve = AnimationCurve.Linear(0,0,1,1);
        chained = true;
    }
}

public class CPC_CameraPath : MonoBehaviour
{

    public bool useMainCamera = true;
    public GameObject selectedCamera;
    public bool lookAtTarget = false;
    public Transform target;
    public bool playOnAwake = false;
    public float playOnAwakeTime = 10;
    public List<CPC_Point> points = new List<CPC_Point>();
    public CPC_Visual visual;
    public bool looped = false;
    public bool alwaysShow = true;
    public CPC_EAfterLoop afterLoop = CPC_EAfterLoop.Continue;
	public GameObject interpreTreadmillData;

    private int currentWaypointIndex;
    private float currentTimeInWaypoint;
    private float timePerSegment;

    private bool paused = false;
    private bool playing = false;

	public float velocity = 0.3f;

	public float distance = 0.0f;

	private float[] pathLens;

	private Vector3 height_position;

    void Start ()
    {
		pathLens = new float[points.Count];

		for (int i = 0; i < points.Count; i++) {
			pathLens [i] = GetLengthSimpsons (0f, 1f, i);
		}

        if (Camera.main == null) { Debug.LogError("There is no main camera in the scene!"); }


	    if (lookAtTarget && target == null)
	    {
	        lookAtTarget = false;
            Debug.LogError("No target selected to look at, defaulting to normal rotation");
        }

	    foreach (var index in points)
	    {
            if (index.curveTypeRotation == CPC_ECurveType.EaseInAndOut) index.rotationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
            if (index.curveTypeRotation == CPC_ECurveType.Linear) index.rotationCurve = AnimationCurve.Linear(0, 0, 1, 1);
            if (index.curveTypePosition == CPC_ECurveType.EaseInAndOut) index.positionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
            if (index.curveTypePosition == CPC_ECurveType.Linear) index.positionCurve = AnimationCurve.Linear(0, 0, 1, 1);
        }

        if (playOnAwake)
            PlayPath(playOnAwakeTime);
    }
		
		

    /// <summary>
    /// Plays the path
    /// </summary>
    /// <param name="time">The time in seconds how long the camera takes for the entire path</param>
    public void PlayPath(float time)
    {
        if (time <= 0) time = 0.001f;
        paused = false;
        playing = true;
        StopAllCoroutines();
        StartCoroutine(FollowPath(time));
    }

    /// <summary>
    /// Stops the path
    /// </summary>
    public void StopPath()
    {
        playing = false;
        paused = false;
        StopAllCoroutines();
    }

    /// <summary>
    /// Allows to change the time variable specified in PlayPath(float time) on the fly
    /// </summary>
    /// <param name="seconds">New time in seconds for entire path</param>
    public void UpdateTimeInSeconds(float seconds)
    {
        timePerSegment = seconds / ((looped) ? points.Count : points.Count - 1);
    }

    /// <summary>
    /// Pauses the camera's movement - resumable with ResumePath()
    /// </summary>
    public void PausePath()
    {
        paused = true;
        playing = false;
    }

    /// <summary>
    /// Can be called after PausePath() to resume
    /// </summary>
    public void ResumePath()
    {
        if (paused)
            playing = true;
        paused = false;
    }

    /// <summary>
    /// Gets if the path is paused
    /// </summary>
    /// <returns>Returns paused state</returns>
    public bool IsPaused()
    {
        return paused;
    }

    /// <summary>
    /// Gets if the path is playing
    /// </summary>
    /// <returns>Returns playing state</returns>
    public bool IsPlaying()
    {
        return playing;
    }

    /// <summary>
    /// Gets the index of the current waypoint
    /// </summary>
    /// <returns>Returns waypoint index</returns>
    public int GetCurrentWayPoint()
    {
        return currentWaypointIndex;
    }

    /// <summary>
    /// Gets the time within the current waypoint (Range is 0-1)
    /// </summary>
    /// <returns>Returns time of current waypoint (Range is 0-1)</returns>
    public float GetCurrentTimeInWaypoint()
    {
        return currentTimeInWaypoint;
    }

    /// <summary>
    /// Sets the current waypoint index of the path
    /// </summary>
    /// <param name="value">Waypoint index</param>
    public void SetCurrentWayPoint(int value)
    {
        currentWaypointIndex = value;
    }

    /// <summary>
    /// Sets the time in the current waypoint 
    /// </summary>
    /// <param name="value">Waypoint time (Range is 0-1)</param>
    public void SetCurrentTimeInWaypoint(float value)
    {
        currentTimeInWaypoint = value;
    }

    /// <summary>
    /// When index/time are set while the path is not playing, this method will teleport the camera to the position/rotation specified
    /// </summary>
    public void RefreshTransform()
    {
		
		selectedCamera.transform.position = GetBezierPosition(currentWaypointIndex, currentTimeInWaypoint);
        if (!lookAtTarget)
            selectedCamera.transform.rotation = GetLerpRotation(currentWaypointIndex, currentTimeInWaypoint);
        else
            selectedCamera.transform.rotation = Quaternion.LookRotation((target.transform.position - selectedCamera.transform.position).normalized);
    }

    IEnumerator FollowPath(float time)
    {
        UpdateTimeInSeconds(time);
        currentWaypointIndex = 0;

		//velocity = 0.5f; 
		//Debug.Log ("global velocity:" + velocity);



        while (currentWaypointIndex < points.Count)
        {
			
            currentTimeInWaypoint = 0;

            while (currentTimeInWaypoint < 1)
            {
				//Debug.Log ("global velocity: " + velocity);
				//Debug.Log ("Distance: " + distance);


				float timePer = 0f;
				timePer = pathLens [currentWaypointIndex] / (0.025f * velocity);


                if (!paused)
                {
					if (velocity == 0f)
						currentTimeInWaypoint = 0f;
					else
                    	currentTimeInWaypoint += 1/timePer;
          
					float next_time = FindTValue (currentTimeInWaypoint, currentWaypointIndex);

					Vector3 nextPosition = GetBezierPosition(currentWaypointIndex, next_time);

					//Debug.Log ("position diff = 0?: " + ((nextPosition - selectedCamera.transform.position) == Vector3.zero) );

					Quaternion lookRot = Quaternion.LookRotation ((nextPosition - selectedCamera.transform.position).normalized);
					if (currentTimeInWaypoint != 1/timePer) {
						selectedCamera.transform.rotation = Quaternion.Lerp (selectedCamera.transform.rotation, lookRot, Time.deltaTime * 1);
					}
					selectedCamera.transform.position = nextPosition;
                }
                yield return 0;
            }


            ++currentWaypointIndex;
            if (currentWaypointIndex == points.Count - 1 && !looped) break;
            if (currentWaypointIndex == points.Count && afterLoop == CPC_EAfterLoop.Continue) currentWaypointIndex = 0;


        }
        StopPath();
    }

    int GetNextIndex(int index)
    {
        if (index == points.Count-1)
            return 0;
        return index + 1;
    }

	//The derivative of cubic De Casteljau's Algorithm
	Vector3 DeCasteljausAlgorithmDerivative(float t, int curr_index)
	{

		int next_index = GetNextIndex(curr_index);
		Vector3 A, B, C, D;
		A = points [curr_index].position;
		B = points[curr_index].position + points[curr_index].handlenext;
		C = points [next_index].position + points [next_index].handleprev;
		D = points [next_index].position;

		Vector3 dU = t * t * (-3f * (A - 3f * (B - C) - D));
		dU += t * (6f * (A - 2f * B + C));
		dU += -3f * (A - B); 

		return dU;
	}

	//Get and infinite small length from the derivative of the curve at position t
	float GetArcLengthIntegrand(float t, int curr_index)
	{
		//The derivative at this point (the velocity vector)
		Vector3 dPos = DeCasteljausAlgorithmDerivative(t, curr_index);

		float integrand = dPos.magnitude;

		return integrand;
	}

	//Get the length of the curve between two t values with Simpson's rule
	float GetLengthSimpsons(float tStart, float tEnd, int curr_index)
	{
		//This is the resolution and has to be even
		int n = 50;

		//Now we need to divide the curve into sections
		float delta = (tEnd - tStart) / (float)n;

		//The main loop to calculate the length

		//Everything multiplied by 1
		float endPoints = GetArcLengthIntegrand(tStart, curr_index) + GetArcLengthIntegrand(tEnd, curr_index);

		//Everything multiplied by 4
		float x4 = 0f;
		for (int i = 1; i < n; i += 2)
		{
			float t = tStart + delta * i;

			x4 += GetArcLengthIntegrand(t, curr_index);
		}

		//Everything multiplied by 2
		float x2 = 0f;
		for (int i = 2; i < n; i += 2)
		{
			float t = tStart + delta * i;

			x2 += GetArcLengthIntegrand(t, curr_index);
		}

		//The final length
		float length = (delta / 3f) * (endPoints + 4f* x4 + 2f * x2);

		return length;
	}

	float FindTValue(float distance_percentage, int curr_index)
	{
		//Need a start value to make the method start
		//Should obviously be between 0 and 1
		//We can say that a good starting point is the percentage of distance traveled

		float t = distance_percentage;

		//Need an error so we know when to stop the iteration
		float error = 0.001f;

		//We also need to avoid infinite loops
		int iterations = 0;

		while (true)
		{
			//Newton's method
			float tNext = t - ((GetLengthSimpsons(0f, t, curr_index) - distance_percentage * pathLens[curr_index]) / GetArcLengthIntegrand(t, curr_index));

			//Have we reached the desired accuracy?
			if (Mathf.Abs(tNext - t) < error)
			{
				break;
			}

			t = tNext;

			iterations += 1;

			if (iterations > 1000)
			{
				break;
			}
		}

		return t;
	}

	Vector3 GetBezierPosition(int pointIndex, float time)
    {
		float t = time; //points[pointIndex].positionCurve.Evaluate(time);
		int nextIndex = GetNextIndex(pointIndex);

        return
            Vector3.Lerp(
                Vector3.Lerp(
                    Vector3.Lerp(points[pointIndex].position,
                        points[pointIndex].position + points[pointIndex].handlenext, t),
                    Vector3.Lerp(points[pointIndex].position + points[pointIndex].handlenext,
                        points[nextIndex].position + points[nextIndex].handleprev, t), t),
                Vector3.Lerp(
                    Vector3.Lerp(points[pointIndex].position + points[pointIndex].handlenext,
                        points[nextIndex].position + points[nextIndex].handleprev, t),
                    Vector3.Lerp(points[nextIndex].position + points[nextIndex].handleprev,
                        points[nextIndex].position, t), t), t);



    }

    private Quaternion GetLerpRotation(int pointIndex, float time)
    {
        return Quaternion.LerpUnclamped(points[pointIndex].rotation, points[GetNextIndex(pointIndex)].rotation, points[pointIndex].rotationCurve.Evaluate(time));
    }

#if UNITY_EDITOR
    public void OnDrawGizmos()
    {
        if (UnityEditor.Selection.activeGameObject == gameObject || alwaysShow)
        {
            if (points.Count >= 2)
            {
                for (int i = 0; i < points.Count; i++)
                {
                    if (i < points.Count - 1)
                    {
                        var index = points[i];
                        var indexNext = points[i + 1];
                        UnityEditor.Handles.DrawBezier(index.position, indexNext.position, index.position + index.handlenext,
                            indexNext.position + indexNext.handleprev,((UnityEditor.Selection.activeGameObject == gameObject) ? visual.pathColor : visual.inactivePathColor), null, 5);
                    }
                    else if (looped)
                    {
                        var index = points[i];
                        var indexNext = points[0];
                        UnityEditor.Handles.DrawBezier(index.position, indexNext.position, index.position + index.handlenext,
                            indexNext.position + indexNext.handleprev, ((UnityEditor.Selection.activeGameObject == gameObject) ? visual.pathColor : visual.inactivePathColor), null, 5);
                    }
                }
            }

            for (int i = 0; i < points.Count; i++)
            {
                var index = points[i];
                Gizmos.matrix = Matrix4x4.TRS(index.position, index.rotation, Vector3.one);
                Gizmos.color = visual.frustrumColor;
                Gizmos.DrawFrustum(Vector3.zero, 90f, 0.25f, 0.01f, 1.78f);
                Gizmos.matrix = Matrix4x4.identity;
            }
        }
    }
#endif

}
