using UnityEngine;

public class UnityShell : MonoBehaviour
{
    public static Main main;

    void Start()
    {
        main = new Main();
       
        main.Start();
    }

    void FixedUpdate()
    {
        main.Tick(Time.fixedDeltaTime);
    }
}