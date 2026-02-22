using __GAME__.Source.Features;
using UnityEngine;

public class UnityShell : MonoBehaviour
{
    public static Main main;

    void Start()
    {
        main = new Main();
        
        main.Add<EventBus>();
        main.Add<GameStateMachine>();
        main.Add<TimeSystem>();
        main.Add<GameManager>();
        main.Add<PlayerFeature>();
        
        main.Start();
    }

    void FixedUpdate()
    {
        main.Tick(Time.fixedDeltaTime);
    }
    
    void OnApplicationQuit()
    {
        main.Save();
    }
}