using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;

public class AdvancedTextEditor
{
	private Regex keywordsregex;
	private Regex autocompleteWordsregex;
	private Regex stringregex;
	private string plaintext;
	private string richtext;
	Vector2 scrollPos = Vector2.zero;
	GUIStyle _overlaystyle;
	
	#region PUBLIC METHODS
	public AdvancedTextEditor(string text = null)
	{
		LoadLanguageSyntax(Application.dataPath + "/csharp.plist");
		plaintext = text;
		if (plaintext==null){plaintext="";}
		richtext = HighlightCode(plaintext);
	}
	
	public string DrawGUI(Rect position)
	{
		//Backup original GUI Colors
        Color backupContentColor = GUI.contentColor;
        Color backupBackgroundColor = GUI.backgroundColor;
		//Get the TextArea Rect
		//Rect textarearect = new Rect(0f, 0f, 0f, 0f);
		Vector2 textareasize = overlaystyle.CalcSize (new GUIContent(richtext));
		Rect contentRect = new Rect(0, 0, Mathf.Max(position.width, textareasize.x), Mathf.Max(position.height, textareasize.y));
		//Wrap the TextArea in a scrollview
		scrollPos = GUI.BeginScrollView(position, scrollPos, contentRect, false, false);
		//Create textarea with transparent text
        GUI.contentColor = Color.clear;
        string newplaintext = GUI.TextArea(contentRect, plaintext);
		
		
        //get the texteditor of the textarea to control selection
        int controlID = GUIUtility.GetControlID(position.GetHashCode(), FocusType.Keyboard);
        TextEditor editor = (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), controlID -1);
		
		if (newplaintext!=plaintext){
			plaintext = newplaintext;
			richtext = HighlightCode(plaintext);	
		}
 
        //set background of all textfield transparent
		GUI.backgroundColor = new Color(1f, 1f, 1f, 0f);
		GUI.contentColor = Color.gray;
		GUI.Label(contentRect, richtext, overlaystyle);
		GUI.EndScrollView();
		//Reset color from backups
        GUI.contentColor = backupContentColor;
        GUI.backgroundColor = backupBackgroundColor;
		
		//Use Tabs
		//if (Event.current.type == EventType.KeyDown && Event.current.keyCode==KeyCode.Tab){
		if (GUIUtility.keyboardControl==editor.controlID && Event.current.keyCode==KeyCode.Tab){
			plaintext = plaintext.Insert(editor.pos, "\t");
			editor.pos+=1;
			editor.SelectNone();
		}
		
		//return edited String
		return plaintext;
	}
	#endregion
	
	#region PRIVATE METHODS
	private string HighlightCode(string code)
	{
		code = autocompleteWordsregex.Replace (code, "<color=red>$0</color>");
		code = keywordsregex.Replace (code, "<color=blue>$0</color>");
		code = stringregex.Replace (code, "<color=green>$0</color>");
		return code;
	}
	
	private void LoadLanguageSyntax(string filePath)
	{
        Hashtable plist = new Hashtable();
        if (PropertyListSerializer.LoadPlistFromFile(filePath, plist)) {
			ArrayList keywords = (ArrayList)plist["keywords"];
			ArrayList autocompleteWords = (ArrayList)plist["autocompleteWords"];
			keywordsregex = new Regex(@"\b("+ string.Join("|", keywords.ToArray().Select(o => o.ToString()).ToArray()) +@")\b");
			autocompleteWordsregex = new Regex(@"\b("+ string.Join("|", autocompleteWords.ToArray().Select(o => o.ToString()).ToArray()) +@")\b");
        	stringregex = new Regex("\"(.*?)\"");
		}
        else { Debug.Log("Unable to open plist file.");}
	}
	
	private GUIStyle overlaystyle
	{
		get{
			if (_overlaystyle!=null){
				return _overlaystyle;
			} else {
				_overlaystyle = new GUIStyle(GUI.skin.textArea);
				_overlaystyle.normal.textColor = Color.white;
				_overlaystyle.wordWrap = false;
				_overlaystyle.clipping = TextClipping.Overflow;
				_overlaystyle.richText = true;
			}
			return _overlaystyle;
		}
	}
	#endregion
}
