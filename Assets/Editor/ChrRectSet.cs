﻿using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/*
 * Unity extension to set a custom font.
 * Set by using the information of fnt file bmfont outputs.
 * 
 * Install:
 * Copy to Assets\Editor\ , this script.
 * 
 * Use:
 * Custom -> Custom Font Setting -> Chr Rect Set.
 * 
 * D&D, "Custom Font", "font texture" and "fnt file(.txt)"
 * Push "Set" button.
 * 
 * or split.
 * D&D, "Custom Font" and "font texture".
 * input, Use Texture Area, Count X, Count Y and Char Length.
 * Push "Set" button.
 * 
 * License:
 * Public Domain.
 */
public class ChrRectSet : EditorWindow {
    public Font customFontObj;
    public TextAsset fontPosTbl;
    public Texture fontTexture;
    public bool xoffsetEnable = true;
    public Vector2 scrollPos;

    public Rect useTexRect = new Rect(0, 0, 256, 256);
    public int fontCountX = 8;
    public int fontCountY = 8;
    public int fontLength = 64;

    struct ChrRect {
        public int id;
        public int x;
        public int y;
        public int w;
        public int h;
        public int xofs;
        public int yofs;

        public int index;
        public float uvX;
        public float uvY;
        public float uvW;
        public float uvH;
        public float vertX;
        public float vertY;
        public float vertW;
        public float vertH;
        public float width;
    }

    // add menu
    [MenuItem("Custom/Custom Font Setting/Chr Rect Set")]
    static void Init() {
        EditorWindow.GetWindow(typeof(ChrRectSet));
    }

    // layout window
    void OnGUI() {
        EditorGUILayout.BeginScrollView(scrollPos);

        // use .fnt(.txt)
        customFontObj = (Font)EditorGUILayout.ObjectField("Custom Font", customFontObj, typeof(Font), false);
        fontTexture = (Texture)EditorGUILayout.ObjectField("Font Texture", fontTexture, typeof(Texture), false);
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Use BMFont fnt File", EditorStyles.boldLabel);
        fontPosTbl = (TextAsset)EditorGUILayout.ObjectField("BMFont fnt (.txt)", fontPosTbl, typeof(TextAsset), false);
        xoffsetEnable = EditorGUILayout.Toggle("xoffset Enable", xoffsetEnable);
        if (GUILayout.Button("Set Character Rects")) {
            if (customFontObj == null) this.ShowNotification(new GUIContent("No Custom Font selected"));
            else if (fontTexture == null) this.ShowNotification(new GUIContent("No Font Texture selected"));
            else if (fontPosTbl == null) this.ShowNotification(new GUIContent("No Font Position Table file selected"));
            else {
                CalcChrRect(customFontObj, fontPosTbl, fontTexture);
            }
        }

        // split 
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Grid Layout", EditorStyles.boldLabel);
        useTexRect = EditorGUILayout.RectField("Use Texture Area", useTexRect);
        fontCountX = EditorGUILayout.IntField("Font Count X", fontCountX);
        fontCountY = EditorGUILayout.IntField("Font Count Y", fontCountY);
        fontLength = EditorGUILayout.IntField("Character Length", fontLength);
        if (GUILayout.Button("Set Character Rects")) {
            if (customFontObj == null) this.ShowNotification(new GUIContent("No Custom Font selected"));
            else if (fontTexture == null) this.ShowNotification(new GUIContent("No Font Texture selected"));
            else CalcChrRectGrid(customFontObj, fontTexture, useTexRect, fontCountX, fontCountY, fontLength);
        }

        EditorGUILayout.EndScrollView();
    }

    void OnInspectorUpdate() {
        this.Repaint();
    }

    // set by .fnt(.txt)
    void CalcChrRect(Font fontObj, TextAsset posTbl, Texture tex) {
        float imgw = (float)tex.width;
        float imgh = (float)tex.height;
        string txt = posTbl.text;
        List<ChrRect> tblList = new List<ChrRect>();
        int asciiStartOffset = int.MaxValue;
        int maxH = 0;
        foreach (string line in txt.Split('\n')) {
            if (line.IndexOf("char id=") == 0) {
                ChrRect d = GetChrRect(line, imgw, imgh);
                if (asciiStartOffset > d.id) asciiStartOffset = d.id;
                if (maxH < d.h) maxH = d.h;
                tblList.Add(d);
            }
        }
        ChrRect[] tbls = tblList.ToArray();

        // index value
        for (int i = 0; i < tbls.Length; i++) {
            tbls[i].index = tbls[i].id - asciiStartOffset;
        }

        // make new CharacterInfo
        SetCharacterInfo(tbls, fontObj);
        // fontObj.asciiStartOffset = asciiStartOffset;

        this.ShowNotification(new GUIContent("Complete"));
    }

    // set by split
    void CalcChrRectGrid(Font fontObj, Texture tex, Rect area, int xc, int yc, int num) {
        float imgw = (float)tex.width;
        float imgh = (float)tex.height;
        int fw = (int)(area.width - area.x) / xc;
        int fh = (int)(area.height - area.y) / yc;
        List<ChrRect> tblList = new List<ChrRect>();
        for (int i = 0; i < num; i++) {
            int xi = i % xc;
            int yi = i / xc;
            ChrRect d = new ChrRect();
            d.index = i;
            d.uvX = (float)(area.x + (fw * xi)) / imgw;
            d.uvY = (float)(imgh - (area.y + (fh * yi) + fh)) / imgh;
            d.uvW = (float)fw / imgw;
            d.uvH = (float)fh / imgh;
            d.vertX = 0;
            d.vertY = 0;
            d.vertW = fw;
            d.vertH = -fh;
            d.width = fw;
            tblList.Add(d);
        }
        ChrRect[] tbls = tblList.ToArray();
        SetCharacterInfo(tbls, fontObj);
        this.ShowNotification(new GUIContent("Complete"));
    }

    // over write custom font by new CharacterInfo
    void SetCharacterInfo(ChrRect[] tbls, Font fontObj) {
        CharacterInfo[] nci = new CharacterInfo[tbls.Length];
        for (int i = 0; i < tbls.Length; i++) {
            nci[i].index = tbls[i].index;
            nci[i].width = tbls[i].width;
            nci[i].uv.x = tbls[i].uvX;
            nci[i].uv.y = tbls[i].uvY;
            nci[i].uv.width = tbls[i].uvW;
            nci[i].uv.height = tbls[i].uvH;
            nci[i].vert.x = tbls[i].vertX;
            nci[i].vert.y = tbls[i].vertY;
            nci[i].vert.width = tbls[i].vertW;
            nci[i].vert.height = tbls[i].vertH;
        }
        fontObj.characterInfo = nci;
    }

    // get font table one line.
    ChrRect GetChrRect(string line, float imgw, float imgh) {
        ChrRect d = new ChrRect();

        foreach (string s in line.Split(' ')) {
            if (s.IndexOf("id=") >= 0) d.id = GetParamInt(s, "id=");
            else if (s.IndexOf("x=") >= 0) d.x = GetParamInt(s, "x=");
            else if (s.IndexOf("y=") >= 0) d.y = GetParamInt(s, "y=");
            else if (s.IndexOf("width=") >= 0) d.w = GetParamInt(s, "width=");
            else if (s.IndexOf("height=") >= 0) d.h = GetParamInt(s, "height=");
            else if (s.IndexOf("xoffset=") >= 0) d.xofs = GetParamInt(s, "xoffset=");
            else if (s.IndexOf("yoffset=") >= 0) d.yofs = GetParamInt(s, "yoffset=");
            else if (s.IndexOf("xadvance=") >= 0) d.width = GetParamInt(s, "xadvance=");
        }

        d.uvX = (float)d.x / imgw;
        d.uvY = (float)(imgh - d.y - d.h) / imgh;
        d.uvW = (float)d.w / imgw;
        d.uvH = (float)d.h / imgh;

        d.vertX = (xoffsetEnable) ? (float)d.xofs : 0.0f;
        d.vertY = -(float)d.yofs;
        d.vertW = d.w;
        d.vertH = -d.h;

        return d;
    }

    // "wd=int" to int
    int GetParamInt(string s, string wd) {
        if (s.IndexOf(wd) >= 0) {
            int v;
            if (int.TryParse(s.Substring(wd.Length), out v)) return v;
        }
        return int.MaxValue;
    }
}
