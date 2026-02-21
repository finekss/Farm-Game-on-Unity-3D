using UnityEngine;

public class UnityShell : MonoBehaviour
{
    public static Main Main;

    void Start()
    {
        Main = new Main();
        Main.Start();
    }

    void FixedUpdate()
    {
        Main.Tick(Time.fixedDeltaTime);
    }
}