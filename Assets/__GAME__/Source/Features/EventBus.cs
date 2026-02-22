using System;
using System.Collections.Generic;

namespace __GAME__.Source.Features
{
    public class EventBus : FeatureBase
    {
        private readonly Dictionary<Type, List<Delegate>> _subscribers = new();

        public void Subscribe<T>(Action<T> callback)
        {
            var type = typeof(T);

            if (!_subscribers.ContainsKey(type))
                _subscribers[type] = new List<Delegate>();

            _subscribers[type].Add(callback);
        }

        public void Unsubscribe<T>(Action<T> callback)
        {
            var type = typeof(T);

            if (_subscribers.ContainsKey(type))
                _subscribers[type].Remove(callback);
        }

        public void Publish<T>(T evt)
        {
            var type = typeof(T);

            if (!_subscribers.ContainsKey(type))
                return;

            foreach (var callback in _subscribers[type])
                ((Action<T>)callback)?.Invoke(evt);
        }
    }
}