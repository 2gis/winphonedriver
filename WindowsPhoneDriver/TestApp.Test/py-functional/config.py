# coding: utf-8
import os

BASE_DIR = os.path.dirname(os.path.dirname(__file__))
AUT_PATH = r"..\TestApp\Bin\x86\Debug\TestApp_Debug_x86.xap"

DESIRED_CAPABILITIES = {
    "app": os.path.abspath(os.path.join(BASE_DIR, AUT_PATH)),
}
