using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using KModkit;
using System.IO;

public enum Order
{
    Standard_Braille_Order,
    Individual_Reading_Order,
    Merged_Reading_Order
}

public class TouchTransmissionScript : MonoBehaviour {

    public KMBombInfo Bomb;
    public KMAudio Audio;
    public KMBombModule Module;

    public KMSelectable playButton;
    public KMSelectable[] inputButtons;
    public GameObject bump;
    public Coroutine[] movements = new Coroutine[7];

    static int moduleIdCounter = 1;
    int moduleId;
    private bool moduleSolved;

    private bool playing;
    int timePointer = -1;
    int submissionPointer = 0;

    private string generatedWord;
    private int shift;
    private string answerWord;
    private Order chosenOrder;
    private List<bool[]> properBraille;
    private bool[] generatedSequence;
    private List<int> solution = new List<int>();
    private bool bumpOut;

    void Awake () {
        moduleId = moduleIdCounter++;
        foreach (KMSelectable button in inputButtons)
            button.OnInteract += delegate () 
            {
                int pos = Array.IndexOf(inputButtons, button);
                if (movements[pos] != null)
                    StopCoroutine(movements[pos]);
                movements[pos] = StartCoroutine(ButtonMove(button, 0, -0.004f, 0.04f, 0.1f));
                Submit(pos);
                return false;
            };
        playButton.OnInteract += delegate () 
        {
            if (movements[6] != null)
                StopCoroutine(movements[6]);
            movements[6] = StartCoroutine(ButtonMove(playButton, 0.0125f, 0.0075f, 0.08f, 0.75f));
            playing = true;
            return false;
        };

    }
    void Start ()
    {
        StartCoroutine(MeasureTimer());
        GetWord();
        ComputeShift();
        GenerateAnswer();
        DoLogging();
    }
    void GetWord()
    {
        chosenOrder = (Order)UnityEngine.Random.Range(0, 3);
        generatedWord = WordList.words.PickRandom().ToUpperInvariant();
        properBraille = BrailleData.WordToBraille(generatedWord);
        generatedSequence = ArrangeOrder(properBraille, chosenOrder);
    }
    void ComputeShift()
    {
        shift = Bomb.GetSerialNumber().Any(x => "AEIOU".Contains(x)) ? -1 : 1;
        foreach (char letter in generatedWord)
            answerWord += (char)((letter - 'A' + shift + 26) % 26 + 'A');
    }
    void GenerateAnswer()
    {
        bool[] fullString = ArrangeOrder(BrailleData.WordToBraille(answerWord), Order.Standard_Braille_Order);
        switch (chosenOrder)
        {
            case Order.Standard_Braille_Order:
                solution = Enumerable.Range(0, fullString.Length).Where(x => fullString[x]).Select(x => x % 6).ToList();
                break;
            case Order.Individual_Reading_Order:
                int[] order = new[] { 0, 3, 1, 4, 2, 5 };
                for (int i = 0; i < fullString.Length; i++)
                    if (fullString[order[i % 6] + 6 * (i / 6)])
                        solution.Add(order[i % 6] % 6);
                Debug.Log(solution.Join());
                break;
            case Order.Merged_Reading_Order:
                int glyphCount = fullString.Length / 6;
                for (int row = 0; row < 3; row++)
                    for (int glyph = 0; glyph < glyphCount; glyph++)
                    {
                        if (fullString[6 * glyph + row])
                            solution.Add(row);
                        if (fullString[6 * glyph + row + 3])
                            solution.Add(row + 3);
                    }
                break;
        }

    }
    void DoLogging()
    {
        Debug.LogFormat("[Touch Transmission #{0}] The generated word is {1}.", moduleId, generatedWord);
        Debug.LogFormat("[Touch Transmission #{0}] The module is reading in {1}. This gives a sequence of {2}.", moduleId, chosenOrder.ToString().Replace('_',' '),
            generatedSequence.Select(b => b ? '•' : '○').Join());
        Debug.LogFormat("[Touch Transmission #{0}] Apply a shift of {1} yields the solution word {2}.", moduleId, shift, answerWord);
        Debug.LogFormat("[Touch Transmission #{0}] The solution sequence (buttons are labelled in Standard Braille Reading Order) is {1}.", moduleId, solution.Select(x => x + 1).Join(""));
    }
    bool[] ArrangeOrder(List<bool[]> input, Order order)
    {
        List<bool> output = new List<bool>();
        switch (order)
        {
            case Order.Standard_Braille_Order:
                foreach (bool[] letter in input)
                    for (int i = 0; i < 6; i++)
                        output.Add(letter[i]);
                break;
            case Order.Individual_Reading_Order:
                int[] numOrder = new[] { 0, 3, 1, 4, 2, 5 };
                foreach (bool[] letter in input)
                    for (int i = 0; i < 6; i++)
                        output.Add(letter[numOrder[i]]);
                break;
            case Order.Merged_Reading_Order:
                for (int i = 0; i < 3; i++)
                    foreach (bool[] letter in input)
                    {
                        output.Add(letter[i]);
                        output.Add(letter[i + 3]);
                    }
                break;
        }
        return output.ToArray();
    }

    void Submit(int pos)
    {
        if (moduleSolved)
            return;
        if (solution[submissionPointer] == pos)
        {
            submissionPointer++;
            Debug.LogFormat("[Touch Transmission #{0}] You inputted {1}, current sequence is {2}.", moduleId, pos + 1, solution.Select(x => x + 1).Take(submissionPointer).Join(""));
            if (submissionPointer >= solution.Count) 
            {
                moduleSolved = true;
                playing = false;
                Module.HandlePass();
                Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.CorrectChime, transform);
                Debug.LogFormat("[Touch Transmission #{0}] Module solved!", moduleId);
                if (bumpOut)
                    StartCoroutine(BumpMove());
            }
        }
        else
        {
            Debug.LogFormat("[Touch Transmission #{0}] Inputted {1} when expected {2}. Strike.", moduleId, pos + 1, solution[submissionPointer] + 1);
            submissionPointer = 0;
            Module.HandleStrike();
        }
    }

    IEnumerator MeasureTimer()
    {
        int prevTime;
        do
        {
            prevTime = (int)Bomb.GetTime();
            yield return null;
            if ((int)Bomb.GetTime() != prevTime)
                HandleTimerTick();
        } while (!moduleSolved);
    }

    void HandleTimerTick()
    {
        if (playing)
        {

            bool prev = (timePointer != -1 ?
                generatedSequence[timePointer] : false);
            timePointer++;
            if (timePointer >= 18)
            {
                playing = false;
                timePointer = -1;
                if (bumpOut)
                    StartCoroutine(BumpMove());
            }
            else
            {
                if (generatedSequence[timePointer] != prev)
                    StartCoroutine(BumpMove());
            }
        }
    }

    IEnumerator BumpMove()
    {
        bumpOut = !bumpOut;
        if (!bumpOut)
        {
            while (bump.transform.localPosition.z > -5)
            {
                bump.transform.localPosition += Vector3.back * Time.deltaTime * 15;
                yield return null;
            }
            bump.transform.localPosition = Vector3.back * 5;
        }
        else
        {
            while (bump.transform.localPosition.z < 0)
            {
                bump.transform.localPosition += Vector3.forward * Time.deltaTime * 15;
                yield return null;
            }
            bump.transform.localPosition = Vector3.zero;
        }
        yield return null;
    }

    IEnumerator ButtonMove(KMSelectable button, float start, float end, float speed, float iPunch)
    {
        button.AddInteractionPunch(iPunch);
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, button.transform);
        while (button.transform.localPosition.y > end)
        {
            button.transform.localPosition += Vector3.down * speed * Time.deltaTime;
            yield return null;
        }
        while (button.transform.localPosition.y < start)
        {
            button.transform.localPosition += Vector3.up * speed * Time.deltaTime;
            yield return null;
        }
    }

    #pragma warning disable 414
    private readonly string TwitchHelpMessage = @"Use [!{0} play] to press the play button. Use [!{0} submit 123456] to press those buttons in Standard Braille Reading Order.";
    #pragma warning restore 414

    IEnumerator ProcessTwitchCommand (string command)
    {
        command = command.Trim().ToUpperInvariant();
        string[] parameters = command.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (command == "PLAY")
        {
            yield return null;
            playButton.OnInteract();
        }
        else if (Regex.IsMatch(command, @"^SUBMIT\s+[1-6]+$"))
        {
            yield return null;
            foreach (int num in parameters.Last().Select(x => x - '1'))
            {
                inputButtons[num].OnInteract();
                yield return new WaitForSeconds(0.1f);
            }
        }
    }
    IEnumerator TwitchHandleForcedSolve ()
    {
        foreach (int num in solution.Skip(submissionPointer))
        {
            inputButtons[num].OnInteract();
            yield return new WaitForSeconds(0.1f);
        }
    }
}
