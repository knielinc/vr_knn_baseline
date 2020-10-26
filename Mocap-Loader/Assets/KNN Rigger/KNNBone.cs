using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class KNNBone : MonoBehaviour
{
    public List<KNNBone> children;
    public KNNBone parent;
    public List<Quaternion> rotations;
    public Vector3 offset;
    public void initKNNBone(string name)
    {
        this.name = name;
        this.children = new List<KNNBone>();
        this.rotations = new List<Quaternion>();
    }
    public void initKNNBone(Transform rootTransform)
    {
        this.name = rootTransform.name;
        this.transform.localPosition = rootTransform.localPosition;
        this.transform.localRotation = rootTransform.localRotation;
        this.offset = this.transform.localPosition;
        this.children = new List<KNNBone>();
        this.rotations = new List<Quaternion>();


        for (int childIdx = 0; childIdx < rootTransform.childCount; childIdx++)
        {
            GameObject childObj = new GameObject();

            KNNBone childBone = (KNNBone)childObj.AddComponent(typeof(KNNBone));
            childBone.parent = this;
            childBone.transform.parent = this.transform;
            childBone.initKNNBone(rootTransform.GetChild(childIdx));
            children.Add(childBone);
        }
    }

    public void DivideOffsetsBy(float factor)
    {
        this.offset /= factor;
        this.transform.localPosition = this.offset;
        foreach (KNNBone child in this.children)
        {
            child.DivideOffsetsBy(factor);
        }
    }

    public void RemoveRotations()
    {
        this.rotations.Clear();
    }
    public Quaternion GetRotation(int index)
    {
        return rotations.ElementAt(index);
    }

    public void SetToRotation(int index)
    {
        this.transform.localRotation = GetRotation(index);
        foreach (KNNBone child in this.children)
        {
            child.SetToRotation(index);
        }
    }

    public void AddRotationsFromTransform(Transform rootTransform)
    {
        if (rootTransform.name == this.name)
        {
            this.rotations.Add(rootTransform.localRotation);
            foreach (KNNBone child in this.children)
            {
                int childIdx = this.children.IndexOf(child);
                child.AddRotationsFromTransform(rootTransform.GetChild(childIdx));
                child.parent = this;
            }
        }
        else
        {
            Debug.LogWarning("ERROR: trying to set rotations from bone " + rootTransform.name + " to " + this.name + " (KNNBONE), skipping children");
        }
    }
    void Start()
    {
    }
    void Update()
    {
    }
}
