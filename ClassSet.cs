using System.Collections;
using System.Collections.Generic;
using System;

public class ClassSet<K, T> where T : IComparable {
    private Dictionary<K, SortedSet<T>> setMap;

    public ClassSet() {
        this.setMap = new Dictionary<K, SortedSet<T>>();
    }

    public void Add(K @class, T element) {
        if (!this.setMap.ContainsKey(@class))
            this.setMap.Add(@class, new SortedSet<T>());

        this.setMap[@class].Add(element);
    }

    public void ChangeClass(K oldClass, K newClass, T element) {
        this.setMap[oldClass].Remove(element);
        this.Add(newClass, element);
    }

    public T RandomFromClass(K @class) {
        T element = this.setMap[@class].Min;
        return element;
    }

    public int ClassSize(K @class) {
        if (!this.setMap.ContainsKey(@class))
            return 0;

        else
            return this.setMap[@class].Count;
    }
}