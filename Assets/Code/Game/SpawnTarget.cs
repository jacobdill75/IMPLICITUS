﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Lambda;
using TypeUtil;
using UnityEngine;
using UnityEngine.UI;

public class SpawnTarget : MonoBehaviour
{
    [SerializeField] private StringEvent lambda;

    [SerializeField] private SymbolManagerTester smt;

    public Shrub<Sum<Combinator, Variable>> goal;

    public BoolRef NoBackApplication;
    public BoolRef NoForwardApplication;
    public UnitEvent success;
    private int arrity;
    public IntEvent arrityEvent;
    

    // Start is called before the first frame update
    void Awake()
    {
        lambda.AddRemovableListener(createTarget, this);
        arrityEvent.AddRemovableListener(i => arrity = i,this);
    }

    private void Start()
    {
        smt.GetComponent<SymbolManager>().onCreateTerm.Add(t => goal = t);
    }

    void createTarget(string s)
    {
        char[] c = {'>'};
        var split = s.Split(c);
        var tmp = smt.Variables;
        smt.Variables = split[0].Skip(1).Where(char.IsLetter).ToList();
        goal = smt.CreateTerm(split[1]);
        smt.Variables = tmp;
    }

    public void createTarget(Shrub<Sum<Combinator, Variable>> t)
    {
        goal = t;
        smt.CreateTerm(goal);
        CheckSuccess();
    }

    public void unApply(Shrub<Sum<Combinator,Variable>> newTerm,LayoutTracker lt, Combinator C, List<int> path)
    {
        StartCoroutine(smt.GetComponent<SymbolManager>().UnTransition(newTerm, lt,C,path,smt.currentLayout));
        goal = newTerm;
        CheckSuccess();
    }

    public void addParens(List<int> path, int size, LayoutTracker paren,Shrub<Sum<Combinator,Variable>> newTerm)
    {
        StartCoroutine(smt.GetComponent<SymbolManager>().BackApplyParens(path,size,paren,smt.currentLayout,newTerm));
    }

    public void CheckSuccess()
    {
        print("checking");
        if (isFinished())
            success.Invoke();
    }
    
    bool isFinished()
    {

        bool isFinished(List<Shrub<Sum<Combinator, Variable>>> term,int arr, int i)
        {
            if (term.Count == 0)
                return arr == i;

            var tail = term.Skip(1).ToList(); //TODO test me
            
            return term[0].Match(l => isFinished(tail,arr,i),
                v => v.Match(c => isFinished(tail,arr,i), x =>
                {
                    if ((int) x == i)
                        return isFinished(tail, arr, i - 1);
                    if ((int) x == -1)
                        return isFinished(tail, arr, i - 1) || isFinished(tail, arr, i);
                    return false;
                })
                );
        }

        goal.Match(l => isFinished(l, arrity,0), v =>
        {
            if (arrity == 1)
                return v.Match(_ => false, x => (int) x <= 0);
            return false;
        });
        return goal.Match<bool>(l =>
        {
            var ll = l.SkipWhile(s => s.Preorder().TrueForAll(v => v.Match(_ => true, x => (int)x == -1))); //ignore combinators at the start
            
            return ll.Select((s, i) => (s, i)).ToList()
                .TrueForAll(pi => 
                    pi.s.Match(_ => false,
                               v => v.Match(_ => false,x => (int)x == pi.i))); //the rest is just the variables in order
            //TODO verify off by ones
        }, x => x.Match(_ => false,i => (int)i == 0));
    }

}
