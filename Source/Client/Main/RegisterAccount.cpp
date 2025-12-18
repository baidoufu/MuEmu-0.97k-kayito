#include "stdafx.h"
#include "RegisterAccount.h"
#include "Protocol.h"

CRegisterAccount gRegisterAccount;

CRegisterAccount::CRegisterAccount()
{
	this->DialogOpen = false;
	memset(this->Account, 0, sizeof(this->Account));
	memset(this->Password, 0, sizeof(this->Password));
	this->ActiveInput = 0;
	this->BoxWidth = 120;
	this->BoxHeight = 22;
	this->DialogWidth = 200.0f;
	this->DialogHeight = 150.0f;
	this->DialogPosX = 0.0f;
	this->DialogPosY = 0.0f;
}

CRegisterAccount::~CRegisterAccount()
{

}

void CRegisterAccount::Init()
{
	// Initialization done via Interface hooks - no direct hooks needed
}

void CRegisterAccount::RenderRegisterButton()
{
	if (SceneFlag != LOG_IN_SCENE || this->DialogOpen)
	{
		return;
	}

	// Position the register button to the left of the login button area
	float ButtonWidth = 80.0f;
	float ButtonHeight = 24.0f;
	float PosX = ImgCenterScreenPosX(ButtonWidth) - 100.0f;
	float PosY = 420.0f; // Use virtual 480-based coordinate (480 - 60 = 420)

	// Render the register button
	DisableAlphaBlend();
	glColor3f(1.0f, 1.0f, 1.0f);
	RenderBitmap(240, PosX, PosY, ButtonWidth, ButtonHeight, (0.0f / 256.0f), (0.0f / 64.0f), (213.0f / 256.0f), (64.0f / 64.0f), true, true);

	if (IsWorkZone((int)PosX, (int)PosY, (int)ButtonWidth, (int)ButtonHeight))
	{
		glColor3f(0.8f, 0.6f, 0.4f);
		EnableAlphaBlend();
		RenderBitmap(240, PosX, PosY, ButtonWidth, ButtonHeight, (0.0f / 256.0f), (0.0f / 64.0f), (213.0f / 256.0f), (64.0f / 64.0f), true, true);
		glColor3f(1.0f, 1.0f, 1.0f);
		DisableAlphaBlend();
	}

	EnableAlphaTest(true);
	SelectObject(m_hFontDC, g_hFont);
	SetBackgroundTextColor = Color4b(255, 255, 255, 0);
	SetTextColor = Color4b(255, 255, 255, 255);
	RenderText((int)PosX, CenterTextPosY("Register", ((int)PosY + ((int)ButtonHeight / 2))), "Register", REAL_WIDTH((int)ButtonWidth), RT3_SORT_CENTER, NULL);
}

bool CRegisterAccount::CheckRegisterButton()
{
	if (SceneFlag != LOG_IN_SCENE || this->DialogOpen)
	{
		return false;
	}

	float ButtonWidth = 80.0f;
	float ButtonHeight = 24.0f;
	float PosX = ImgCenterScreenPosX(ButtonWidth) - 100.0f;
	float PosY = 420.0f; // Use virtual 480-based coordinate (480 - 60 = 420)

	if (IsWorkZone((int)PosX, (int)PosY, (int)ButtonWidth, (int)ButtonHeight))
	{
		if (MouseLButton && MouseLButtonPush)
		{
			MouseLButtonPush = false;
			MouseUpdateTime = 0;
			MouseUpdateTimeMax = 6;
			PlayBuffer(25, 0, 0);

			this->OpenRegisterDialog();
			return true;
		}
	}

	return false;
}

void CRegisterAccount::OpenRegisterDialog()
{
	this->DialogOpen = true;
	memset(this->Account, 0, sizeof(this->Account));
	memset(this->Password, 0, sizeof(this->Password));
	this->ActiveInput = 0;

	// Center the dialog
	this->DialogPosX = ImgCenterScreenPosX(this->DialogWidth);
	this->DialogPosY = ImgCenterScreenPosY(this->DialogHeight);
}

void CRegisterAccount::CloseRegisterDialog()
{
	this->DialogOpen = false;
	memset(this->Account, 0, sizeof(this->Account));
	memset(this->Password, 0, sizeof(this->Password));
	this->ActiveInput = 0;
}

bool CRegisterAccount::IsDialogOpen()
{
	return this->DialogOpen;
}

void CRegisterAccount::RenderRegisterDialog()
{
	if (!this->DialogOpen)
	{
		return;
	}

	float PosX = this->DialogPosX;
	float PosY = this->DialogPosY;
	float Width = this->DialogWidth;
	float Height = this->DialogHeight;

	// Draw dialog background
	EnableAlphaTest(true);
	glColor4f(0.0f, 0.0f, 0.0f, 0.8f);
	RenderColor(PosX, PosY, Width, Height);
	glColor4f(0.5f, 0.5f, 0.5f, 1.0f);
	RenderColor(PosX, PosY, Width, 2.0f); // Top border
	RenderColor(PosX, PosY + Height - 2.0f, Width, 2.0f); // Bottom border
	RenderColor(PosX, PosY, 2.0f, Height); // Left border
	RenderColor(PosX + Width - 2.0f, PosY, 2.0f, Height); // Right border
	glColor4f(1.0f, 1.0f, 1.0f, 1.0f);
	DisableAlphaBlend();
	glEnable(GL_TEXTURE_2D);

	// Title
	float TitleY = PosY + 10.0f;
	EnableAlphaTest(true);
	SelectObject(m_hFontDC, g_hFont);
	SetBackgroundTextColor = Color4b(255, 255, 255, 0);
	SetTextColor = Color4b(255, 200, 100, 255);
	RenderText((int)PosX, (int)TitleY, "Account Registration", REAL_WIDTH((int)Width), RT3_SORT_CENTER, NULL);

	// Account label and input
	float LabelY = TitleY + 30.0f;
	SetTextColor = Color4b(255, 255, 255, 255);
	RenderText((int)(PosX + 10.0f), (int)LabelY, "Account:", REAL_WIDTH(60), RT3_SORT_LEFT, NULL);

	float InputX = PosX + 80.0f;
	float InputW = Width - 90.0f;
	this->RenderInputBox(InputX, LabelY - 3.0f, InputW, 20.0f, 0);

	// Password label and input
	float PassY = LabelY + 30.0f;
	RenderText((int)(PosX + 10.0f), (int)PassY, "Password:", REAL_WIDTH(60), RT3_SORT_LEFT, NULL);
	this->RenderInputBox(InputX, PassY - 3.0f, InputW, 20.0f, 1);

	// Confirm button
	float ButtonY = PassY + 40.0f;
	float ButtonW = 70.0f;
	float ButtonH = 24.0f;
	float ConfirmX = PosX + (Width / 2.0f) - ButtonW - 10.0f;

	this->RenderBox(ConfirmX, ButtonY, ButtonW, ButtonH);
	SetTextColor = Color4b(255, 255, 255, 255);
	RenderText((int)ConfirmX, CenterTextPosY("Confirm", ((int)ButtonY + ((int)ButtonH / 2))), "Confirm", REAL_WIDTH((int)ButtonW), RT3_SORT_CENTER, NULL);

	// Cancel button
	float CancelX = PosX + (Width / 2.0f) + 10.0f;
	this->RenderBox(CancelX, ButtonY, ButtonW, ButtonH);
	RenderText((int)CancelX, CenterTextPosY("Cancel", ((int)ButtonY + ((int)ButtonH / 2))), "Cancel", REAL_WIDTH((int)ButtonW), RT3_SORT_CENTER, NULL);
}

bool CRegisterAccount::CheckRegisterDialog()
{
	if (!this->DialogOpen)
	{
		return false;
	}

	float PosX = this->DialogPosX;
	float PosY = this->DialogPosY;
	float Width = this->DialogWidth;
	float LabelY = PosY + 40.0f;
	float InputX = PosX + 80.0f;
	float InputW = Width - 90.0f;
	float InputH = 20.0f;

	// Check account input click
	if (IsWorkZone((int)InputX, (int)(LabelY - 3.0f), (int)InputW, (int)InputH))
	{
		if (MouseLButton && MouseLButtonPush)
		{
			MouseLButtonPush = false;
			PlayBuffer(25, 0, 0);
			this->ActiveInput = 0;
			return true;
		}
	}

	// Check password input click
	float PassY = LabelY + 30.0f;
	if (IsWorkZone((int)InputX, (int)(PassY - 3.0f), (int)InputW, (int)InputH))
	{
		if (MouseLButton && MouseLButtonPush)
		{
			MouseLButtonPush = false;
			PlayBuffer(25, 0, 0);
			this->ActiveInput = 1;
			return true;
		}
	}

	// Check buttons
	float ButtonY = PassY + 40.0f;
	float ButtonW = 70.0f;
	float ButtonH = 24.0f;
	float ConfirmX = PosX + (Width / 2.0f) - ButtonW - 10.0f;
	float CancelX = PosX + (Width / 2.0f) + 10.0f;

	// Confirm button
	if (IsWorkZone((int)ConfirmX, (int)ButtonY, (int)ButtonW, (int)ButtonH))
	{
		if (MouseLButton && MouseLButtonPush)
		{
			MouseLButtonPush = false;
			PlayBuffer(25, 0, 0);

			// Validate input
			if (strlen(this->Account) >= 4 && strlen(this->Password) >= 4)
			{
				this->SendRegisterAccount();
				this->CloseRegisterDialog();
			}
			else
			{
				CreateOkMessageBox("Account and password must be at least 4 characters!");
			}
			return true;
		}
	}

	// Cancel button
	if (IsWorkZone((int)CancelX, (int)ButtonY, (int)ButtonW, (int)ButtonH))
	{
		if (MouseLButton && MouseLButtonPush)
		{
			MouseLButtonPush = false;
			PlayBuffer(25, 0, 0);
			this->CloseRegisterDialog();
			return true;
		}
	}

	// Block clicks within dialog area
	if (IsWorkZone((int)PosX, (int)PosY, (int)Width, (int)this->DialogHeight))
	{
		return true;
	}

	return false;
}

void CRegisterAccount::HandleKeyInput(WPARAM wParam)
{
	if (!this->DialogOpen)
	{
		return;
	}

	char* target = (this->ActiveInput == 0) ? this->Account : this->Password;
	int maxLen = 10;
	int curLen = (int)strlen(target);

	if (wParam == VK_BACK)
	{
		if (curLen > 0)
		{
			target[curLen - 1] = 0;
		}
	}
	else if (wParam == VK_TAB)
	{
		this->ActiveInput = (this->ActiveInput == 0) ? 1 : 0;
	}
	else if (wParam == VK_RETURN)
	{
		if (strlen(this->Account) >= 4 && strlen(this->Password) >= 4)
		{
			this->SendRegisterAccount();
			this->CloseRegisterDialog();
		}
	}
	else if (wParam == VK_ESCAPE)
	{
		this->CloseRegisterDialog();
	}
	else if (wParam >= 0x20 && wParam <= 0x7E && curLen < maxLen)
	{
		// Printable ASCII character
		target[curLen] = (char)wParam;
		target[curLen + 1] = 0;
	}
}

void CRegisterAccount::SendRegisterAccount()
{
	PMSG_REGISTER_ACCOUNT_SEND pMsg;

	pMsg.header.setE(0xF1, 0x06, sizeof(pMsg));

	PacketArgumentEncrypt((BYTE*)pMsg.account, (BYTE*)this->Account, (sizeof(this->Account) - 1));
	PacketArgumentEncrypt((BYTE*)pMsg.password, (BYTE*)this->Password, (sizeof(this->Password) - 1));

	pMsg.TickCount = GetTickCount();

	pMsg.ClientVersion[0] = (*(BYTE*)(0x0055961C)) - 1;
	pMsg.ClientVersion[1] = (*(BYTE*)(0x0055961D)) - 2;
	pMsg.ClientVersion[2] = (*(BYTE*)(0x0055961E)) - 3;
	pMsg.ClientVersion[3] = (*(BYTE*)(0x0055961F)) - 4;
	pMsg.ClientVersion[4] = (*(BYTE*)(0x00559620)) - 5;

	memcpy(pMsg.ClientSerial, (void*)0x00559624, sizeof(pMsg.ClientSerial));

	gProtocol.DataSend((BYTE*)&pMsg, pMsg.header.size);
}

void CRegisterAccount::RenderBox(float PosX, float PosY, float Width, float Height)
{
	DisableAlphaBlend();

	glColor3f(1.0f, 1.0f, 1.0f);

	RenderBitmap(240, PosX, PosY, Width, Height, (0.0f / 256.0f), (0.0f / 64.0f), (213.0f / 256.0f), (64.0f / 64.0f), true, true);

	if (IsWorkZone((int)PosX, (int)PosY, (int)Width, (int)Height))
	{
		glColor3f(0.8f, 0.6f, 0.4f);

		EnableAlphaBlend();

		RenderBitmap(240, PosX, PosY, Width, Height, (0.0f / 256.0f), (0.0f / 64.0f), (213.0f / 256.0f), (64.0f / 64.0f), true, true);

		glColor3f(1.0f, 1.0f, 1.0f);

		DisableAlphaBlend();
	}
}

void CRegisterAccount::RenderInputBox(float PosX, float PosY, float Width, float Height, int InputIdx)
{
	// Draw input box background
	EnableAlphaTest(true);

	if (this->ActiveInput == InputIdx)
	{
		glColor4f(0.3f, 0.3f, 0.4f, 1.0f);
	}
	else
	{
		glColor4f(0.2f, 0.2f, 0.2f, 1.0f);
	}

	RenderColor(PosX, PosY, Width, Height);

	// Draw border
	glColor4f(0.6f, 0.6f, 0.6f, 1.0f);
	RenderColor(PosX, PosY, Width, 1.0f);
	RenderColor(PosX, PosY + Height - 1.0f, Width, 1.0f);
	RenderColor(PosX, PosY, 1.0f, Height);
	RenderColor(PosX + Width - 1.0f, PosY, 1.0f, Height);

	glColor4f(1.0f, 1.0f, 1.0f, 1.0f);
	DisableAlphaBlend();
	glEnable(GL_TEXTURE_2D);

	// Draw text
	char* text = (InputIdx == 0) ? this->Account : this->Password;
	char displayText[12] = { 0 };

	if (InputIdx == 1)
	{
		// Password - show asterisks
		int len = (int)strlen(text);
		for (int i = 0; i < len && i < 10; i++)
		{
			displayText[i] = '*';
		}
	}
	else
	{
		strncpy(displayText, text, sizeof(displayText) - 1);
	}

	EnableAlphaTest(true);
	SetBackgroundTextColor = Color4b(255, 255, 255, 0);
	SetTextColor = Color4b(255, 255, 255, 255);
	RenderText((int)(PosX + 5.0f), (int)(PosY + 3.0f), displayText, REAL_WIDTH((int)(Width - 10.0f)), RT3_SORT_LEFT, NULL);
}
