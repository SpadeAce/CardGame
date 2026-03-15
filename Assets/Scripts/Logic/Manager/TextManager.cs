using UnityEngine;

/// <summary>
/// 현지화 텍스트 매니저.
/// TextManager.Instance.Get(alias)로 현재 언어에 맞는 문자열을 반환한다.
/// </summary>
public class TextManager : Singleton<TextManager>
{
    private const string LangKey = "Language";

    private TextTable _table;

    public GameData.Language CurrentLanguage { get; private set; }

    public void Initialize()
    {
        _table = new TextTable();
        _table.Load();
        CurrentLanguage = (GameData.Language)PlayerPrefs.GetInt(LangKey, (int)GameData.Language.Kor);
    }

    public void SetLanguage(GameData.Language language)
    {
        CurrentLanguage = language;
        PlayerPrefs.SetInt(LangKey, (int)language);
    }

    /// <summary>
    /// alias에 해당하는 현재 언어 문자열을 반환한다.
    /// 미등록 alias는 alias 문자열 그대로 반환.
    /// </summary>
    public string Get(string alias)
    {
        var data = _table.Get(alias);
        if (data == null) return alias;
        return CurrentLanguage switch
        {
            GameData.Language.Eng => data.Eng,
            GameData.Language.Jpn => data.Jpn,
            _ => data.Kor
        };
    }
}
