﻿using LibMMD.Unity3D;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

public class PMXModelLoader
{
    public async static Task<Transform> LoadPMXModel(string path, RuntimeAnimatorController runtimeAnimatorController, Transform parent, bool autoShowModel = true)
    {
        if (!File.Exists(path))
        {
            UnityEngine.Debug.Log(path);
            UnityEngine.Debug.Log("与えられたパスにファイルが存在ません");
            return null;
        }

        MMDModel mmdModel = null;
        try
        {
            mmdModel = await MMDModel.ImportModel(path, autoShowModel);
        }
        catch(Exception ex)
        {
            UnityEngine.Debug.Log(ex.Message);
            return null;
        }

        if (mmdModel == null) { return null; }

        mmdModel.transform.parent = parent;
        mmdModel.transform.localPosition = Vector3.zero;
        mmdModel.transform.localRotation = Quaternion.identity;

        try
        {
            AvatarMaker avaterMaker = mmdModel.gameObject.AddComponent<AvatarMaker>();
            avaterMaker.Prepare(mmdModel, runtimeAnimatorController);
            await avaterMaker.MakeAvatar();

#if UNITY_EDITOR
            GameObject.DestroyImmediate(avaterMaker);
#else
        GameObject.Destroy(avaterMaker);
#endif

        }
        catch (Exception ex)
        {
            UnityEngine.Debug.Log("アバターの作成に失敗しました");

#if UNITY_EDITOR
            GameObject.DestroyImmediate(mmdModel.gameObject);
#else
        GameObject.Destroy(mmdModel.gameObject);
#endif

            return null;
        }
        
        return mmdModel.transform;
    }

    public async static Task<Transform> LoadPMXModel(string path, RuntimeAnimatorController runtimeAnimatorController, bool autoShowModel = true)
    {
         return await LoadPMXModel(path, runtimeAnimatorController, null, autoShowModel);
    }

    public async static Task<Transform> LoadPMXModel(string path, RuntimeAnimatorController runtimeAnimatorController)
    {
        return await LoadPMXModel(path, runtimeAnimatorController, null);
    }

    public async static Task<Transform> LoadPMXModel(string path, bool autoShowModel = true)
    {
        return await LoadPMXModel(path, null, null, autoShowModel);
    }
}