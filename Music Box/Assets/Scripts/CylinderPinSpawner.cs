using UnityEngine;

public class CylinderPinSpawner : MonoBehaviour
{
    [Header("References")]
    public Transform cylinder;         
    public GameObject pinPrefab;      

    [Header("Layout")]
    public int noOfRows = 5;         
    public int distanceRefernce = 25;         

    public int noOfPitch = 16;          
    public float radius = 0.5f;        
    public float height = 1.0f;        



    private Transform pinsParent;

    void Start()
    {
        SpawnGrid();
    }

    public void SpawnGrid()
    {
        if (cylinder == null || pinPrefab == null) return;

        if (pinsParent == null)
        {
            GameObject go = new GameObject("Pins");
            go.transform.SetParent(cylinder, false);
            pinsParent = go.transform;
        }


        for (int step = 0; step < noOfRows; step++)
        {
            float frac = step / (float)distanceRefernce;
            float angleRad = frac * Mathf.PI * 2f + Mathf.Deg2Rad;

            var rando = Random.Range(0, 1);
            for (int pitch = 0; pitch < rando; pitch++)
            {
                Vector3 localPos = AnglePitchToLocalPosition(angleRad, pitch);
                GameObject pin = Instantiate(pinPrefab, pinsParent);
                pin.transform.localPosition = localPos;

                // face outward
                Vector3 outward = new Vector3(localPos.x, 0f, localPos.z).normalized;
                if (outward.sqrMagnitude < 1e-6f)
                    outward = Vector3.forward;
                pin.transform.localRotation = Quaternion.LookRotation(outward, Vector3.up);
            }
        }
    }

    Vector3 AnglePitchToLocalPosition(float angleRad, int pitchIndex)
    {
        float x = Mathf.Cos(angleRad) * radius;
        float z = Mathf.Sin(angleRad) * radius;

        float rowHeight = height / Mathf.Max(1, noOfPitch);
        float y = -height * 0.5f + (pitchIndex + 0.5f) * rowHeight;

        return new Vector3(x, y, z);
    }

}
