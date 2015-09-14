# coding: utf-8
import os

BUILD_CONFIG = os.environ.get('BUILD_CONFIG', 'Debug')

BASE_DIR = os.path.dirname(os.path.dirname(__file__))
AUT_PATH = r"..\TestApp\Bin\x86\{0}\TestApp_{0}_x86.xap".format(BUILD_CONFIG)

DESIRED_CAPABILITIES = {
    "app": os.path.abspath(os.path.join(BASE_DIR, AUT_PATH)),
}
