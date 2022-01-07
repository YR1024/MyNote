"""
Unit tests to ensure that we don't change PyAutoGUI in such a way
that makes the text in "Automate the Boring Stuff with Python" wrong.
"""

import unittest
import pyautogui


class TestGeneral(unittest.TestCase):
    def test_failsafe(self):
        """From page 415 of the 1st edition:
        >>> import pyautogui
        >>> pyautogui.PAUSE = 1
        >>> pyautogui.FAILSAFE = True
        """
        oldValue = pyautogui.PAUSE  # PAUSE should exist.
        pyautogui.PAUSE = 1  # This should not fail.
        pyautogui.PAUSE = oldValue

        oldValue = pyautogui.FAILSAFE  # FAILSAFE should exist.
        pyautogui.FAILSAFE = False  # This should not fail.
        pyautogui.FAILSAFE = oldValue



    def test_size(self):
        """From page 415 of 1st edition:
        >>> import pyautogui
        >>> pyautogui.size()
        (1920, 1080)
        >>> width, height = pyautogui.size()

        From page 476 of the second edition:
        >>> import pyautogui
        >>> wh = pyautogui.size() # Obtain the screen resolution.
        >>> wh
        Size(width=1920, height=1080)
        >>> wh[0]
        1920
        >>> wh.width
        1920
        """
        wh = pyautogui.size()
        self.assertTrue(type(wh[0]), int)
        self.assertTrue(type(wh[1]), int)

        self.assertTrue(type(wh.width), int)
        self.assertTrue(type(wh.height), int)
