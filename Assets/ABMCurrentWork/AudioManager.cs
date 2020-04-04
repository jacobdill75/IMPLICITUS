﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{

    /*   Variables   */
    [SerializeField] IntEvent effectInt; //Get information on which effect to play
    [SerializeField] IntEvent songInt;   //Get information on which song to play

    [SerializeField] GameObject[] effectPrefabsList;  //List of effect prefabs given by user
    [SerializeField] GameObject[] songPrefabsList;    //List of song prefabs given by user

    //Holds audio and their tag
    struct AudioObject
    {
        public int tag;
        public GameObject audioPrefab;

        //Constructor for AudioObject
        public AudioObject(int Tag, GameObject AudioPrefab)
        {
            tag = Tag;
            audioPrefab = AudioPrefab;
        }
    };

    //Variables to hold audio objects
    private List<AudioObject> effectPoolList; //Save all instances of effects 
    private Queue<GameObject> songQueue;   //Will hold current song playing

    //used to ensure onlyone instance of AudioManager exists
    public static AudioManager instance;


    /*        Implementation        */
    private void Awake()
    {
        //Ensure Only one instance of AudioManager Exists
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
        DontDestroyOnLoad(gameObject);
        
        //Create variables 
        effectPoolList = new List<AudioObject>();
        songQueue = new Queue<GameObject>();

        //Listen for command to play audio
        songInt.AddListener(playSong);
        effectInt.AddListener(playEffect);

    }

    //Used dby listener of effectEvent
    //Plays event specified
    private void playEffect(int num)
    {
        //Verify effect exists in the effectPrefabList provided by user
        if (0 <= num && num < effectPrefabsList.Length)
        {
            //Check if object is in list *Note: Could probably be more efficient, may edit
            foreach (AudioObject effect in effectPoolList)
            {
                AudioSource sound = effect.audioPrefab.GetComponent<AudioSource>(); //Instance of audiosource
                
                //if effectPrefab being called is in our list and not playing, play it
                if (effect.tag == num && !(sound.isPlaying))
                {
                    sound.Play();
                    return;
                }
            }
            //Effect not found in list, so new one will be created and added
            AudioObject audioObject = new AudioObject(num, (GameObject)Instantiate(effectPrefabsList[num], gameObject.transform));
            effectPoolList.Add(audioObject);
            audioObject.audioPrefab.GetComponent<AudioSource>().Play();
        }
    }
        
    //Used by listener of songInt Event
    //Plays song given by input, turning off song that is currently playing and destroying 
    //If -1 is inputed, mutes current song
    private void playSong(int num)
    {
        //Verify song exists in the songPrefabList provided by user,
        //and if input is -1
        if (-1 <= num && num < songPrefabsList.Length)
        {
            GameObject song; //Holder for songs played or removed

            //If playing song, create new GameObject for song and add to the queue
            if (num >= 0)
            {
                song = (GameObject)Instantiate(songPrefabsList[num], gameObject.transform);
                song.GetComponent<AudioSource>().Play();
                songQueue.Enqueue(song);

                //If only one song is in queue, do not remove the song
                if (songQueue.Count == 1) { return; }
            }

            //Removing song, only if song is playing
            if (songQueue.Count > 0)
            {
                //Remove song previously playing, stop and destroy
                song = songQueue.Dequeue();
                song.GetComponent<AudioSource>().Stop();
                Destroy(song);

                //Clears effectsPoolList (assummed some scene change occurs with song change)
                clearEffectPrefabs();
            }
        }
        else
        {
            Debug.Log("Song" + num + " does not exist in songPrefabList!");
        }

    }

    //Clears all effects prefab Objects 
    private void clearEffectPrefabs()
    {
        foreach (AudioObject effect in effectPoolList)
        {
            Destroy(effect.audioPrefab);
        }
        effectPoolList.Clear(); //Clear whole list
    }
}
