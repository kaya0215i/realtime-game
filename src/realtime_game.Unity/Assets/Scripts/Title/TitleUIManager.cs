using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TitleUIManager : MonoBehaviour {
    [SerializeField] private SaveManager saveManager;

    // ロード画像
    [SerializeField] private Image loadingImage;
    // エラーテキスト
    [SerializeField] private Text errorText;

    // ユーザーログイン用
    [SerializeField] private GameObject loginUI;
    [SerializeField] private Text loginIdInputField;
    [SerializeField] private Text loginPasswordInputField;
    [SerializeField] private Toggle autoLoginToggle;

    // ユーザー登録用
    [SerializeField] private GameObject registerUI;
    [SerializeField] private Text registerIdInputField;
    [SerializeField] private Text registerPasswordInputField;
    [SerializeField] private Text registerConfirmPasswordInputField;
    [SerializeField] private Text displayNameInputField;

    private bool isLoading = false;

    private void Start() {
        loginUI.SetActive(false);
        registerUI.SetActive(false);
        errorText.text = "";
    }

    private void Update() {
        Loading();
    }

    /// <summary>
    /// ロード画像の処理
    /// </summary>
    private void Loading() {
        if (isLoading) {
            loadingImage.rectTransform.eulerAngles += new Vector3(0, 0, 2);
        }
    }

    /// <summary>
    /// ロード画像の表示変更
    /// </summary>
    public void IsLoading(bool status) {
        isLoading = status;

        if (isLoading) {
            loadingImage.gameObject.SetActive(true);

            loginUI.SetActive(false);
            registerUI.SetActive(false);
        }
        else {
            loadingImage.gameObject.SetActive(false);
            loadingImage.rectTransform.eulerAngles = Vector3.zero;

            loginUI.SetActive(true);
            registerUI.SetActive(false);
        }
    }

    /// <summary>
    /// ログイン画面にするボタン
    /// </summary>
    public void OnClickGotoLoginBtn() {
        loginUI.SetActive(true);
        registerUI.SetActive(false);
    }

    /// <summary>
    /// ユーザー登録画面にするボタン
    /// </summary>
    public void OnClickGotoRegisterBtn() {
        loginUI.SetActive(false);
        registerUI.SetActive(true);
    }

    /// <summary>
    /// ログインボタン
    /// </summary>
    public async void OnClickLogin() {
        // ロード画像の表示
        IsLoading(true);

        // 空かどうかチェック
        if (loginIdInputField.text == "" ||
           loginPasswordInputField.text == "") {
            errorText.text = "ログインID、パスワードが空白です。";
            Debug.Log("ログインID、パスワードが空白です。");

            // ロード画像の非表示
            IsLoading(false);
            return;
        }

        bool result = await UserModel.Instance.LoginUserAsync(loginIdInputField.text, loginPasswordInputField.text);

        if (result) {
            Debug.Log("ログイン成功");
            // ユーザー情報保存
            saveManager.SaveData(loginIdInputField.text, UserModel.Instance.HashPassword(loginPasswordInputField.text), autoLoginToggle.isOn);

            // ロード画像の非表示
            IsLoading(false);

            // ロビーシーンに移動
            SceneManager.LoadScene("LobyScene");
            return;
        }
        else {
            errorText.text = "ログインID、パスワードが間違っています。";
            Debug.Log("ログインID、パスワードが間違っています。");

            // ロード画像の非表示
            IsLoading(false);
            return;
        }
    }

    /// <summary>
    /// ユーザー登録ボタン
    /// </summary>
    public async void OnClickRegister() {
        // ロード画像の表示
        IsLoading(true);

        // 空かどうかチェック
        if (registerIdInputField.text == "" ||
           registerPasswordInputField.text == "" ||
           registerConfirmPasswordInputField.text == "" ||
           displayNameInputField.text == "") {
            errorText.text = "ログインID、パスワード、ゲーム内の名前を空白にすることは出来ません。";
            Debug.Log("ログインID、パスワード、ゲーム内の名前を空白にすることは出来ません。");

            // ロード画像の非表示
            IsLoading(false);
            return;
        }

        // パスワードが8文字以上かどうか
        if(registerPasswordInputField.text.Length < 8) {
            errorText.text = "パスワードは8文字以上で入力してください。";
            Debug.Log("パスワードは8文字以上で入力してください。");

            // ロード画像の非表示
            IsLoading(false);
            return;
        }

        // パスワードと確認用パスワードが同じかどうか
        if(registerPasswordInputField.text != registerConfirmPasswordInputField.text) {
            errorText.text = "確認用パスワードが正しくありません。";
            Debug.Log("確認用パスワードが正しくありません。");

            // ロード画像の非表示
            IsLoading(false);
            return;
        }

        bool result = await UserModel.Instance.RegistUserAsync(registerIdInputField.text, registerPasswordInputField.text, displayNameInputField.text);

        if(result) {
            Debug.Log("ユーザー登録成功");

            // ロード画像の非表示
            IsLoading(false);

            // ロビーシーンに移動
            SceneManager.LoadScene("LobyScene");
            return;
        }
        else {
            errorText.text = "ログインIDが他のユーザーと被っているため登録できませんでした。";
            Debug.Log("ログインIDが他のユーザーと被っているため登録できませんでした。");

            // ロード画像の非表示
            IsLoading(false);
            return;
        }
    }
}
