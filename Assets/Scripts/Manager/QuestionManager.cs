using UnityEngine;
using System.Collections.Generic;

[DefaultExecutionOrder(-10)]
public class QuestionManager : MonoBehaviour
{
    public static QuestionManager Instance { get; private set; }

    [System.Serializable]
    public class Question
    {
        public int CorrectPwd;
        public List<(string, string)> Hints;
    }

    public static readonly List<Question> questionSet = new List<Question>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        LoadQuestions();
    }

    void LoadQuestions()
    {
        questionSet.Add(new Question
        {
            CorrectPwd = 893,
            Hints = new List<(string, string)>
            {
                ("243", "1 Correct Number, Position is Correct"),
                ("259", "1 Correct Number, Position is Wrong"),
                ("382", "2 Correct Numbers, Positions are all Wrong"),
                ("174", "ALL Numbers are Wrong")
            }
        });

        questionSet.Add(new Question
        {
            CorrectPwd = 352,
            Hints = new List<(string, string)>
            {
                ("458", "1 Correct Number, Position is Correct"),
                ("437", "1 Correct Number, Position is Wrong"),
                ("213", "2 Correct Numbers, Positions are all Wrong"),
                ("351", "2 Correct Numbers, Positions are all Correct")
            }
        });

        questionSet.Add(new Question
        {
            CorrectPwd = 520,
            Hints = new List<(string, string)>
            {
                ("310", "1 Correct Number, Position is Correct"),
                ("372", "1 Correct Number, Position is Wrong"),
                ("053", "2 Correct Numbers, Positions are all Wrong"),
                ("491", "ALL Numbers are Wrong")
            }
        });

        questionSet.Add(new Question
        {
            CorrectPwd = 682,
            Hints = new List<(string, string)>
            {
                ("976", "1 Correct Number, Position is Wrong"),
                ("518", "1 Correct Number, Position is Wrong"),
                ("592", "1 Correct Number1, Position is Correct"),
                ("739", "ALL Numbers are Wrong"),
                ("265", "2 Correct Numbers, Positions are all Wrong")
            }
        });

        questionSet.AsReadOnly();
    }

    public (int, List<(string, string)>) RandomPassword()
    {
        int password = Random.Range(1000, 9999);
        List<(string, string)> hints = new List<(string, string)>
        {
            ($"{(password % 10).ToString()}", "The 4th digit."),
            ($"{(password / 10 % 10).ToString()}", "The 3rd digit."),
            ($"{(password / 100 % 10).ToString()}", "The 2nd digit."),
            ($"{(password / 1000 % 10).ToString()}", "The 1st digit.")
        };
        return (password, hints);
    }

    public (int, List<(string, string)>) GetRandomQuestion()
    {
        if (questionSet.Count == 0)
            return default;

        float chance = Random.Range(0f, 1f);
        // if (chance < 0.2f)
        // {
        //     Debug.Log("Generated Random Password");
        //     return RandomPassword();
        // }
        // int index = Random.Range(0, questionSet.Count);
        // return (questionSet[index].CorrectPwd, questionSet[index].Hints);
        Debug.Log("Generated Random Password");
        return RandomPassword();
    }
}