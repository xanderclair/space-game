using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FreeList<T>
{
    public struct Element
    {
        public Element(T e, int n)
        {
            element = e;
            next = n;
        }
        public T element;
        public int next;
    }
    private List<Element> data = new List<Element>();
    private int firstFree = -1;

    public int Insert(T element)
    {
        if (firstFree != -1)
        {
            int index = firstFree;
            firstFree = data[firstFree].next;
            data[index] = new Element(element, data[index].next);
            return index;
        }
        else
        {
            Element e;
            e.element = element;
            e.next = 0;
            data.Add(e);
            return data.Count - 1;
        }
    }
    public void Erase(int n)
    {
        Element element = data[n];
        element.next = firstFree;
        firstFree = n;
    }

    public void Clear()
    {
        data.Clear();
        firstFree = -1;
    }
    public int Size()
    {
        return data.Count;
    }
    public T this[int i]
    {
        set
        {
            data[i] = new Element(value, firstFree);
        }
        get
        {
            return data[i].element;
        }
    }
}