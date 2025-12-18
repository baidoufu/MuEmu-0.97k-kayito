#pragma once

class CRegisterAccount
{
public:

	CRegisterAccount();

	virtual ~CRegisterAccount();

	void Init();

	void RenderRegisterButton();

	bool CheckRegisterButton();

	void RenderRegisterDialog();

	bool CheckRegisterDialog();

	void OpenRegisterDialog();

	void CloseRegisterDialog();

	bool IsDialogOpen();

	void HandleKeyInput(WPARAM wParam);

	void SendRegisterAccount();

private:

	void RenderBox(float PosX, float PosY, float Width, float Height);

	void RenderInputBox(float PosX, float PosY, float Width, float Height, int InputIdx);

private:

	bool DialogOpen;

	char Account[11];

	char Password[11];

	int ActiveInput; // 0 = account, 1 = password

	int BoxWidth;

	int BoxHeight;

	float DialogPosX;

	float DialogPosY;

	float DialogWidth;

	float DialogHeight;
};

extern CRegisterAccount gRegisterAccount;
