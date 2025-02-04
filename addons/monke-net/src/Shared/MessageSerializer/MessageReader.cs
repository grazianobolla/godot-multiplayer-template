﻿using Godot;
using MonkeNet.NetworkMessages;
using MonkeNet.Shared;
using System.IO;

namespace MonkeNet.Serializer;

public class MessageReader(MemoryStream stream) : BinaryReader(stream)
{
    public T[] ReadArray<T>(MessageTypeEnum collectionType) where T : IPackableElement
    {
        var instance = MessageSerializer.Types[(byte)collectionType];
        if (instance is not IPackableElement) { throw new MonkeNetException($"Registered type {instance.GetType().Name} doesn't implement {typeof(IPackableElement).Name}"); }
        var packableElement = instance as IPackableElement;

        int collectionSize = ReadInt32();
        var res = new T[collectionSize];

        for (int i = 0; i < collectionSize; i++)
        {
            instance.ReadBytes(this); // Read bytes and update internal state
            res[i] = (T)packableElement.GetCopy();
        }

        return res;
    }

    public Vector3 ReadVector3()
    {
        return new(ReadSingle(), ReadSingle(), ReadSingle());
    }

    public Transform3D ReadTransform()
    {
        return new()
        {
            Origin = ReadVector3(),
            Basis = new Basis(ReadVector3(), ReadVector3(), ReadVector3())
        };
    }

    public T ReadPackable<T>() where T : IPackableMessage, new()
    {
        T res = new();
        res.ReadBytes(this);
        return res;
    }
}