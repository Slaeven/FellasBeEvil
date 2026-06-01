using System.Threading;
using Unity.VisualScripting;
using UnityEngine;

public class Fade : MonoBehaviour
{
    [SerializeField] private float timeOut = 3f;


    // Update is called once per frame
    void Update()
    {
        if(timeOut > 0)
        {
            timeOut = timeOut - Time.deltaTime;
        }

        if(timeOut <= 0)
        {
            Destroy(this.gameObject);
        }
    }
}
