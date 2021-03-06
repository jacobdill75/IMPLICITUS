﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager instance;

#pragma warning disable 0649
    [SerializeField] private float fadeTime;
    [SerializeField] private float textDelay; //Delay between each letter of text
    [SerializeField] private Color deselectedColor;

    [SerializeField] private CanvasGroup dim;
    [SerializeField] private CanvasGroup dialogueGroup;
    [SerializeField] private Image charL;
    [SerializeField] private Image charR;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private IntEvent songPlay;
    [SerializeField] private IntEvent effectPlay; //Event Calls audio sound
#pragma warning restore 0649

    private Animator animL;
    private Animator animR;

    private string leftName = "";
    private string rightName = "";

    private string dialogueScript = "";

    private bool pauseContinue = false;

    private Coroutine routine;

    private void Awake()
    {

        songPlay.Invoke(-2); //Save the current Track Playing
        songPlay.Invoke(2);  //Plays client discussion Track

        dim.alpha = 0;
        dim.blocksRaycasts = false;
        dialogueGroup.alpha = 0;
        dialogueGroup.blocksRaycasts = false;
        dialogueGroup.interactable = false;

        if(instance == null)
        {
            instance = this;
        }
        else if(instance != this)
        {
            Destroy(this);
        }

        animL = charL.GetComponent<Animator>();
        animR = charR.GetComponent<Animator>();
    }

    public void SetDialogue(string dialogue)
    {
        dialogueScript = dialogue;
    }

    public void PlayDialogue(string hint)
    {
        if(routine == null)
        {
            routine = StartCoroutine(ProcessDialogue(hint));
        }
    }

    public void PlayDialogue(string dialogue, string hint)
    {
        SetDialogue(dialogue);
        PlayDialogue(hint);
    }

    public void PlayerInput()
    {
        pauseContinue = true;
    }

    private enum DialogueScriptAction
    {
        dialogue,
        sprite,
        name,
        animation
    }

    private class DialogueScriptObject
    {
        public DialogueScriptAction action;
        public string data;

        public DialogueScriptObject(DialogueScriptAction act, string dat)
        {
            action = act;
            data = dat;
        }
    }

    private IEnumerator ProcessDialogue(string hint)
    {
        yield return StartCoroutine(FadeIn());
        string[] lines = dialogueScript.Split(';');
        string[] hints = hint.Split(';');
        for(int i = 0; i < lines.Length - 1; i++)
        {
            string[] data = lines[i].Split(':');
            bool isLeft = true;
            if (data[0].ToLower().Contains("r")) isLeft = false;
            Queue<DialogueScriptObject> dialogueQueue = new Queue<DialogueScriptObject>();
            string txt = "";

            for (int j = 0; j < data[1].Length; j++)
            {
                if(data[1][j] == '<')
                {
                    if (txt.Length > 0)
                    {
                        dialogueQueue.Enqueue(new DialogueScriptObject(DialogueScriptAction.dialogue, txt));
                        txt = "";
                    }
                    int len = data[1].IndexOf('>', j) - j - 1;
                    string action = data[1].Substring(j + 1, len);
                    string[] split = action.Split('=');
                    DialogueScriptAction act = DialogueScriptAction.dialogue;
                    switch (split[0].ToLower())
                    {
                        case "sprite":
                            act = DialogueScriptAction.sprite;
                            break;
                        case "name":
                            act = DialogueScriptAction.name;
                            break;
                        case "animation":
                            act = DialogueScriptAction.animation;
                            break;
                        default:
                            break;
                    }
                    dialogueQueue.Enqueue(new DialogueScriptObject(act, split[1]));
                    j += len + 1;
                }
                else
                {
                    txt += data[1][j];
                }

                if(j == data[1].Length - 1 && txt.Length > 0)
                {
                    dialogueQueue.Enqueue(new DialogueScriptObject(DialogueScriptAction.dialogue, txt));
                    txt = "";
                }
            }


            yield return StartCoroutine(PlayLine(dialogueQueue, isLeft));
        }

        
        //hint
        Queue<DialogueScriptObject> q = new Queue<DialogueScriptObject>();
        
        if (0 < hints.Length)
        {
            print("here");
            q.Enqueue(new DialogueScriptObject(DialogueScriptAction.name,"hint"));
            //q.Enqueue(new DialogueScriptObject(DialogueScriptAction.sprite,"hint"));
            
            foreach (string text in hints)
            {
                q.Enqueue(new DialogueScriptObject(DialogueScriptAction.dialogue,text));
            }
        }

        charL.sprite = null;
        charR.sprite = null;
        yield return StartCoroutine(PlayLine(q, true));
        
        
        
        //unload scene
        yield return StartCoroutine(FadeOut());
        songPlay.Invoke(-3);                //Playing Saved Song
        LoadManager.instance.UnloadSceneAsync("Dialogue");
    }

    private IEnumerator PlayLine(Queue<DialogueScriptObject> line, bool isLeft)
    {
        bool firstText = false;

        while(line.Count > 0)
        {
            DialogueScriptObject o = line.Dequeue();
            switch (o.action)
            {
                case DialogueScriptAction.dialogue:
                    if (!firstText)
                    {
                        firstText = true;
                        dialogueText.text = "";
                        //Show name
                        if (isLeft)
                        {
                            charL.color = Color.white;
                            charR.color = deselectedColor;
                        }
                        else
                        {
                            charR.color = Color.white;
                            charL.color = deselectedColor;
                        }
                        if (charR.sprite == null) charR.color = Color.clear;
                        if (charL.sprite == null) charL.color = Color.clear;
                    }
                    if (isLeft) nameText.text = leftName;
                    else nameText.text = rightName;
                    yield return StartCoroutine(PlayText(o.data));
                    break;
                case DialogueScriptAction.sprite:
                    ChangeSprite(o.data, isLeft);
                    break;
                case DialogueScriptAction.name:
                    ChangeName(o.data, isLeft);
                    break;
                case DialogueScriptAction.animation:
                    PlayAnimation(o.data, isLeft);
                    break;
                default:
                    Debug.LogError("Invalid DialogueScriptAction from dialogue script import!");
                    break;
            }
        }
        if (firstText)
        {
            pauseContinue = false;
            //pause until player input;
            while (!pauseContinue)
            {
                yield return new WaitForEndOfFrame();
            }
            pauseContinue = false;
        }
    }

    private void ChangeSprite(string sprite, bool isLeft)
    {
        if (isLeft)
        {
            charL.sprite = Resources.Load<Sprite>(sprite);
        }
        else
        {
            charR.sprite = Resources.Load<Sprite>(sprite);
        }
    }

    private void ChangeName(string name, bool isLeft)
    {
        if (isLeft)
        {
            leftName = name;
        }
        else
        {
            rightName = name;
        }
    }

    private void PlayAnimation(string anim, bool isLeft)
    {
        if (isLeft)
        {
            animL.SetTrigger(anim);
        }
        else
        {
            animR.SetTrigger(anim);
        }
    }

    private IEnumerator PlayText(string txt)
    {

        effectPlay.Invoke(5); // Play Dialouge Sound

        for (int i = 0; i < txt.Length; i++)
        {
            dialogueText.text += txt[i];
            if(!pauseContinue) yield return new WaitForSeconds(textDelay);
        }

        effectPlay.Invoke(5); // Stop Dialouge Sound

    }

    private IEnumerator FadeIn()
    {
        float timer = 0;
        while (timer < fadeTime * 1.5f)
        {
            dim.alpha = Mathf.Clamp01(timer / fadeTime);
            dialogueGroup.alpha = Mathf.Clamp01((timer - fadeTime / 2) / fadeTime);
            yield return new WaitForEndOfFrame();
            timer += Time.deltaTime;
        }
        dim.blocksRaycasts = true;
        dialogueGroup.blocksRaycasts = true;
        dialogueGroup.interactable = true;
    }

    private IEnumerator FadeOut()
    {
        float timer = 0;
        while (timer < fadeTime * 1.5f)
        {
            dialogueGroup.alpha = 1 - Mathf.Clamp01(timer / fadeTime);
            dim.alpha = 1 - Mathf.Clamp01((timer - fadeTime / 2) / fadeTime);
            yield return new WaitForEndOfFrame();
            timer += Time.deltaTime;
        }
        dim.blocksRaycasts = false;
        dialogueGroup.blocksRaycasts = false;
        dialogueGroup.interactable = false;
    }
}
