using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

public class GameData
{
    public bool hasSave;

    public SerializableVector3 playerPosition;
    public SerializableQuaternion playerRotation;
    
    [System.Serializable]
    public struct SerializableVector3
    {
        public float x;
        public float y;
        public float z;

        public SerializableVector3(Vector3 v)
        {
            x = v.x;
            y = v.y;
            z = v.z;
        }

        public Vector3 ToVector3()
        {
            return new Vector3(x, y, z);
        }
    }
    
    [System.Serializable]
    public struct SerializableQuaternion
    {
        public float x;
        public float y;
        public float z;
        public float w;

        public SerializableQuaternion(Quaternion q)
        {
            x = q.x;
            y = q.y;
            z = q.z;
            w = q.w;
        }

        public Quaternion ToQuaternion()
        {
            return new Quaternion(x, y, z, w);
        }
    }
}

public class Main
{
    public GameData data = new GameData();
    public List<IFeature> features = new List<IFeature>();

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