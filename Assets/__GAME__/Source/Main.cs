using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

public class GameData
{
}

public class Main
{
    public GameData data = new GameData();
    public List<IFeature> features = new List<IFeature>();

    public void Start()
    {
        var jsopn = PlayerPrefs.GetString("save", "{}");
        data = JsonConvert.DeserializeObject<GameData>(jsopn);

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
    protected Main main;

    public virtual void Start()
    {
    }

    public virtual void Setup(Main main)
    {
        this.main = main;
    }

    public virtual void Tick(float dt)
    {
    }

    public virtual void OnSave()
    {
    }
}