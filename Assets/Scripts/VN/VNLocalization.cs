using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

[Serializable] public class VNLanguage { public string code; public string name; }
[Serializable] public class VNLanguageList { public VNLanguage[] languages; }
[Serializable] public class VNTranslationFile { public int schemaVersion; public string scriptId; public string locale; public VNTranslationEntry[] entries; }
[Serializable] public class VNTranslationEntry { public string key; public string sourceHash; public string status; public string text; public VNTranslationSegment[] segments; }
[Serializable] public class VNTranslationSegment { public string source; public string text; }
public readonly struct VNSubtitleSentence
{
    public readonly string Primary, Secondary;
    public VNSubtitleSentence(string primary,string secondary) { Primary=primary;Secondary=secondary; }
}

public sealed class VNLocalizedLine
{
    public readonly string Source;
    readonly Dictionary<string,VNTranslationEntry> translations = new Dictionary<string,VNTranslationEntry>();
    public VNLocalizedLine(string source) { Source=source??""; }
    public void Add(string locale,VNTranslationEntry entry) { if(entry!=null) translations[locale]=entry; }
    public string Text(string locale)
    {
        if(locale==VNSubtitleSettings.Chinese)return Source;
        return translations.TryGetValue(locale,out var entry)?entry.text:null;
    }
    public string PrimaryText => Text(VNSubtitleSettings.PrimaryLanguage)??Source;
    public string SecondaryText
    {
        get
        {
            if(VNSubtitleSettings.SecondaryLanguage==VNSubtitleSettings.Off)return null;
            string actualPrimary=Text(VNSubtitleSettings.PrimaryLanguage)==null?VNSubtitleSettings.Chinese:VNSubtitleSettings.PrimaryLanguage;
            return actualPrimary==VNSubtitleSettings.SecondaryLanguage?null:Text(VNSubtitleSettings.SecondaryLanguage);
        }
    }
    public List<VNSubtitleSentence> Sentences()
    {
        var result=new List<VNSubtitleSentence>();
        if(string.IsNullOrWhiteSpace(SecondaryText))return result;
        translations.TryGetValue(VNSubtitleSettings.PrimaryLanguage,out var primary);
        translations.TryGetValue(VNSubtitleSettings.SecondaryLanguage,out var secondary);
        var template=primary?.segments??secondary?.segments;
        if(template==null||template.Length==0)
        {
            result.Add(new VNSubtitleSentence(PrimaryText,SecondaryText));return result;
        }
        string primaryCode=Text(VNSubtitleSettings.PrimaryLanguage)==null?VNSubtitleSettings.Chinese:VNSubtitleSettings.PrimaryLanguage;
        for(int i=0;i<template.Length;i++)
        {
            string p=Segment(primaryCode,primary,template,i);
            string s=Segment(VNSubtitleSettings.SecondaryLanguage,secondary,template,i);
            if(p==null||s==null)
            {
                result.Clear();result.Add(new VNSubtitleSentence(PrimaryText,SecondaryText));return result;
            }
            result.Add(new VNSubtitleSentence(p,s));
        }
        return result;
    }
    static string Segment(string locale,VNTranslationEntry entry,VNTranslationSegment[] template,int i)
    {
        if(locale==VNSubtitleSettings.Chinese)return template[i].source;
        if(entry?.segments==null||entry.segments.Length!=template.Length||entry.segments[i].source!=template[i].source)return null;
        return entry.segments[i].text;
    }
}

public static class VNLocalization
{
    static VNLanguage[] languages;
    static readonly Dictionary<string,Dictionary<string,VNTranslationEntry>> files=new Dictionary<string,Dictionary<string,VNTranslationEntry>>();
    static readonly HashSet<string> warned=new HashSet<string>();
    public static int ResourceLoadCount { get; private set; }
    public static VNLanguage[] Languages
    {
        get
        {
            if(languages!=null)return languages;
            var asset=Resources.Load<TextAsset>("Localization/VN/languages");
            VNLanguageList list=null;
            try { if(asset!=null)list=JsonUtility.FromJson<VNLanguageList>(asset.text); }
            catch(Exception ex){Debug.LogWarning("[VN localization] Invalid language registry; using Chinese. "+ex.Message);}
            var valid=new List<VNLanguage> {new VNLanguage{code=VNSubtitleSettings.Chinese,name="简体中文"}};
            if(list?.languages!=null)foreach(var language in list.languages)
                if(language!=null&&SafeCode(language.code)&&language.code!=VNSubtitleSettings.Chinese&&!valid.Exists(v=>v.code==language.code)&&!string.IsNullOrWhiteSpace(language.name))valid.Add(language);
            languages=valid.ToArray();return languages;
        }
    }
    public static bool Supports(string locale) => Array.Exists(Languages,l=>l.code==locale);
    public static string Key(string scriptId,int scene,int dialogue,DialogueEntry entry)
        => string.IsNullOrEmpty(entry.id)?scriptId+"/s"+scene+"/d"+dialogue:scriptId+"/"+entry.id;
    public static VNLocalizedLine Resolve(string scriptId,string key,string source,string legacyJapanese=null)
    {
        var result=new VNLocalizedLine(source);
        string hash=Hash(source);
        foreach(var language in Languages)
        {
            if(language.code==VNSubtitleSettings.Chinese)continue;
            var catalogue=Load(scriptId,language.code);
            if(catalogue.TryGetValue(key,out var entry))
            {
                if(entry.sourceHash==hash&&ValidEntry(entry,source))result.Add(language.code,entry);
                else WarnOnce(scriptId+"/"+language.code+"/"+key,"[VN localization] Stale or invalid translation ignored: "+key+" ("+language.code+")");
            }
        }
        if(result.Text(VNSubtitleSettings.Japanese)==null&&!string.IsNullOrWhiteSpace(legacyJapanese))
            result.Add(VNSubtitleSettings.Japanese,new VNTranslationEntry{text=legacyJapanese});
        return result;
    }
    public static VNLocalizedLine Legacy(string source,string japanese)
    {
        var line=new VNLocalizedLine(source);
        if(!string.IsNullOrWhiteSpace(japanese))line.Add(VNSubtitleSettings.Japanese,new VNTranslationEntry{text=japanese});
        return line;
    }
    public static string Hash(string source)
    {
        using(var sha=SHA256.Create())
        {
            var bytes=sha.ComputeHash(Encoding.UTF8.GetBytes((source??"").Replace("\r\n","\n")));
            return BitConverter.ToString(bytes).Replace("-","").ToLowerInvariant();
        }
    }
    public static bool ValidEntry(VNTranslationEntry entry,string source)
    {
        if(entry==null||entry.status!="ready"||string.IsNullOrWhiteSpace(entry.text))return false;
        if(entry.segments==null||entry.segments.Length==0)return false;
        var original=new StringBuilder();var translated=new StringBuilder();
        foreach(var segment in entry.segments)
        {
            if(segment==null||string.IsNullOrEmpty(segment.source)||string.IsNullOrWhiteSpace(segment.text))return false;
            original.Append(segment.source);translated.Append(segment.text);
        }
        return original.ToString()==source&&translated.ToString()==entry.text;
    }
    static Dictionary<string,VNTranslationEntry> Load(string scriptId,string locale)
    {
        string cache=locale+"/"+scriptId;
        if(files.TryGetValue(cache,out var result))return result;
        result=new Dictionary<string,VNTranslationEntry>();files[cache]=result;
        if(!SafeCode(scriptId)||!SafeCode(locale))return result;
        ResourceLoadCount++;
        var asset=Resources.Load<TextAsset>("Localization/VN/"+locale+"/"+scriptId);
        if(asset==null)return result;
        try
        {
            var file=JsonUtility.FromJson<VNTranslationFile>(asset.text);
            if(file.schemaVersion!=1||file.scriptId!=scriptId||file.locale!=locale)throw new FormatException("Header mismatch");
            if(file.entries!=null)foreach(var entry in file.entries)
            {
                if(entry==null||string.IsNullOrEmpty(entry.key))continue;
                if(result.ContainsKey(entry.key))throw new FormatException("Duplicate translation key: "+entry.key);
                result.Add(entry.key,entry);
            }
        }
        catch(Exception ex){result.Clear();WarnOnce(cache,"[VN localization] Invalid catalogue ignored: "+cache+" "+ex.Message);}
        return result;
    }
    static bool SafeCode(string value)
    {
        if(string.IsNullOrEmpty(value))return false;
        foreach(char c in value)if(!char.IsLetterOrDigit(c)&&c!='-'&&c!='_')return false;
        return true;
    }
    static void WarnOnce(string key,string message){if(warned.Add(key))Debug.LogWarning(message);}
    public static void ClearCache(){files.Clear();warned.Clear();languages=null;ResourceLoadCount=0;}
}
