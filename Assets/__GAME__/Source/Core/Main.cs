using System;
using System.Collections.Generic;
using __GAME__.Source.Save;
using Newtonsoft.Json;
using UnityEngine;


public class Main
{   
    private Dictionary<Type, object> services = new();
    public static GameData data = new GameData();
    public List<IFeature> features = new List<IFeature>();
    
    
    public static Main Instance { get; private set; }
    
    public Main()
    {
        Instance = this;
        data = new GameData();
    }
    public void Start()
    {
        var jsonOpen = PlayerPrefs.GetString("save", "{}");
        data = JsonConvert.DeserializeObject<GameData>(jsonOpen);

        foreach (var f in features)
            f.Start();
    }

    public void Tick(float dt)
    {
        foreach (var f in features)
            f.Tick(dt);
    }

    public void Add<T>() where T : class, IFeature, new()
    {
        var feature = new T();
        feature.Setup(this);
        features.Add(feature);
    }

    public T Get<T>() where T : class, IFeature, new()
    {
        foreach (var f in features)
            if (f is T ft)
                return ft;
        return null;
    }
    public void RegisterService<T>(T service)
    {
        services[typeof(T)] = service;
    }

    public T GetService<T>()
    {
        return (T)services[typeof(T)];
    }

    public void Save()
    {
        foreach (var f in features)
        {
            f.OnSave();
        }

        PlayerPrefs.SetString("save", JsonConvert.SerializeObject(data));
    }
}

public interface IFeature
{
    public void Start();
    public void Setup(Main main);
    public void Tick(float dt);
    public void OnSave();
}

public class FeatureBase : IFeature
{
    protected Main Main;

    public virtual void Start()
    {
    }

    public virtual void Setup(Main main)
    {
        this.Main = main;
    }

    public virtual void Tick(float dt)
    {
    }

    public virtual void OnSave()
    {
    }
}