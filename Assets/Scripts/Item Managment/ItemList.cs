using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemList : MonoBehaviour
{

    public GameObject FNS;
    public GameObject P90;


    public static Dictionary<ushort, GameObject> items = new Dictionary<ushort, GameObject>(128);
    public static Dictionary<ushort, Vector3> itemViewModelScale = new Dictionary<ushort, Vector3>(128);
    public static Dictionary<ushort, Vector3> itemViewModelPos = new Dictionary<ushort, Vector3>(128);
    public static Dictionary<ushort, Quaternion> itemViewModelRot = new Dictionary<ushort, Quaternion>(128);

    private void Start()
    {

        items.Add(1, P90);
        Vector3 p90Scale = new Vector3(1f, 1f, 1f);
        itemViewModelScale.Add(1, p90Scale);
        Vector3 p90Pos = new Vector3(0.32267f, -0.46556f, 1.0981f);
        itemViewModelPos.Add(1, p90Pos);
        Quaternion p90Rot = Quaternion.Euler(-90f, 0f, -90f);
        itemViewModelRot.Add(1, p90Rot);

        items.Add(2, FNS);
        Vector3 fnsScale = new Vector3(0.1005924f, 0.1005924f, 0.1005924f);
        itemViewModelScale.Add(2, fnsScale);
        Vector3 fnsPos = new Vector3(0.32267f, -0.46556f, 1.0981f);
        itemViewModelPos.Add(2, fnsPos);
        Quaternion fnsRot = Quaternion.Euler(-90f, 0f, -90f);
        itemViewModelRot.Add(2, fnsRot);


    }


}
