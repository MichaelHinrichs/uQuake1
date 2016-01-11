﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public class BSP29map
{
    private BinaryReader BSPfile;
    public BSPHeader header;

    public BSPColors palette;

    public BSPEntityLump entityLump;
    public BSPFaceLump facesLump;
    public BSPEdgeLump edgeLump;
    public BSPVertexLump vertLump;
    public BSPTexInfoLump texinfoLump;
    public BSPMipTexLump miptexLump;
    public BSPModelLump modelLump;

    public BSP29map(string filename)
    {
        BSPfile = new BinaryReader(File.Open("Assets/Resources/Maps/" + filename, FileMode.Open));
        header = new BSPHeader(BSPfile);
        palette = new BSPColors();

        ReadEntities();
        ReadFaces();
        ReadEdges();
        ReadVerts();
        ReadTexinfo();
        ReadTextures();
        ReadModels();


        BSPfile.BaseStream.Dispose();
    }

    private void ReadVerts()
    {
        vertLump = new BSPVertexLump();
        BSPfile.BaseStream.Seek(header.directory[3].offset, SeekOrigin.Begin);
        int numVerts = header.directory[3].length / 12;
        vertLump.verts = new Vector3[numVerts];
        for (int i = 0; i < numVerts; i++)
        {
            vertLump.AddVert(new Vector3(BSPfile.ReadSingle(), BSPfile.ReadSingle(), BSPfile.ReadSingle()));
        }
    }

    private void ReadEntities()
    {
        BSPfile.BaseStream.Seek(header.directory[0].offset, SeekOrigin.Begin);
        entityLump = new BSPEntityLump(BSPfile.ReadChars(header.directory[0].length));
    }

    private void ReadEdges()
    {
        edgeLump = new BSPEdgeLump();
        BSPfile.BaseStream.Seek(header.directory[12].offset, SeekOrigin.Begin);
        int numEdges = header.directory[12].length / 4;
        edgeLump.edges = new BSPEdge[numEdges];
        for (int i = 0; i < numEdges; i++)
        {
            edgeLump.edges[i] = new BSPEdge(BSPfile.ReadUInt16(), BSPfile.ReadUInt16());
        }

        BSPfile.BaseStream.Seek(header.directory[13].offset, SeekOrigin.Begin);
        int numLedges = header.directory[13].length / 4;
        edgeLump.ledges = new int[numLedges];
        for (int i = 0; i < numLedges; i++)
        {
            edgeLump.ledges[i] = BSPfile.ReadInt32();
        }
    }

    private void ReadFaces()
    {
        facesLump = new BSPFaceLump();
        BSPfile.BaseStream.Seek(header.directory[7].offset, SeekOrigin.Begin);
        int numFaces = header.directory[7].length / 20;
        facesLump.faces = new BSPFace[numFaces];
        // I do seeking inside the loop because I only want to rip the data I care about,
        // and the seek skips over things I won't need, like plane information.
        for (int i = 0; i < numFaces; i++)
        {
            BSPfile.BaseStream.Seek(4, SeekOrigin.Current);
            facesLump.faces[i] = (new BSPFace(BSPfile.ReadInt32(), BSPfile.ReadInt16(), BSPfile.ReadInt16()));
            BSPfile.BaseStream.Seek(8, SeekOrigin.Current);
        }
    }

    private void ReadTexinfo()
    {
        texinfoLump = new BSPTexInfoLump();
        BSPfile.BaseStream.Seek(header.directory[6].offset, SeekOrigin.Begin);
        int numTexinfos = header.directory[6].length / 40;
        texinfoLump.texinfo = new BSPTexInfo[numTexinfos];
        for (int i = 0; i < numTexinfos; i++)
        {
            texinfoLump.texinfo[i] = new BSPTexInfo(new Vector3(BSPfile.ReadSingle(), BSPfile.ReadSingle(), BSPfile.ReadSingle()), BSPfile.ReadSingle(), new Vector3(BSPfile.ReadSingle(), BSPfile.ReadSingle(), BSPfile.ReadSingle()), BSPfile.ReadSingle(), BSPfile.ReadInt32(), BSPfile.ReadInt32());
        }
    }

    private void ReadTextures()
    {
        // Read the texture lump header and the offsets for the textures
        BSPfile.BaseStream.Seek(header.directory[2].offset, SeekOrigin.Begin);
        miptexLump = new BSPMipTexLump(BSPfile.ReadInt32());
        
        
        for (int i = 0; i < miptexLump.tex_count; i++)
        {
            
            miptexLump.LoadOffset((int)BSPfile.ReadUInt32());
        }

        // Now use those offsets and create the texture objects
        for (int i = 0; i < miptexLump.tex_count; i++)
        {
            BSPfile.BaseStream.Seek(header.directory[2].offset + miptexLump.tex_offsets[i], SeekOrigin.Begin);
            miptexLump.texture_headers[i] = new BSPMipTexture(BSPfile.ReadChars(16), (int)BSPfile.ReadUInt32(), (int)BSPfile.ReadUInt32(), (int)BSPfile.ReadUInt32());
        }
        // Now use those texture objects and the palette to make our Texture2D
        // objects that can be used directly
        for (int i = 0; i < miptexLump.tex_count; i++)
        {
            miptexLump.textures[i] = new Texture2D(miptexLump.texture_headers[i].width, miptexLump.texture_headers[i].height);
            miptexLump.textures[i].name = miptexLump.texture_headers[i].name;
            Color32[] colors = new Color32[miptexLump.texture_headers[i].PixelCount()];
            BSPfile.BaseStream.Seek(header.directory[2].offset + miptexLump.tex_offsets[i] + miptexLump.texture_headers[i].offset, SeekOrigin.Begin);
            for (int j = 0; j < miptexLump.texture_headers[i].PixelCount(); j++)
            {
                int index = (int)BSPfile.ReadByte();
                colors[j] = palette.colors[index];
            }
            miptexLump.textures[i].SetPixels32(colors);
            miptexLump.textures[i].filterMode = FilterMode.Point;
            miptexLump.textures[i].Apply();
        }
    }

    private void ReadModels()
    {
        modelLump = new BSPModelLump();
        BSPfile.BaseStream.Seek(header.directory[14].offset, SeekOrigin.Begin);
        int modelCount = header.directory[14].length / 64;
        modelLump.models = new BSPModel[modelCount];
        for (int i = 0; i < modelCount; i++)
        {
            BSPfile.BaseStream.Seek(36, SeekOrigin.Current);
            modelLump.models[i] = new BSPModel(new int[] { BSPfile.ReadInt32(), BSPfile.ReadInt32(), BSPfile.ReadInt32(), BSPfile.ReadInt32() }, BSPfile.ReadInt32());
            BSPfile.BaseStream.Seek(8, SeekOrigin.Current);
        }
    }
}
