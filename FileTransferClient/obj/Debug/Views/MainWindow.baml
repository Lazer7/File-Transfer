   M S B A M L   `   `   `  ����M  IFileTransferClient, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null7 clr-namespace:FileTransferClientFileTransferClient  5   	   #    FileTransferClient.MainWindow   5   	   \ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    5   	   R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5   	   @d2http://schemas.microsoft.com/expression/blend/2008     5   	   Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     5   	   2local clr-namespace:FileTransferClient     5   	     � Title$(  "Lazer's Peer to Peer Transfer File��5   	   $	��400��64   $	��800��6A   .��5	      � ��5
   
   �� 5       �� EndPoint$ 0.5,1&�6"    �� 
StartPoint$ 0.5,0&�63   .��5      �  � Color$ Black��6    � Offset$ 0Q�6-   � 5      $ 	#FFEB1C1C��6   $ 1Q�61   5      .+�5   
   �� -   5        $��ConnectingB��5      $��Connect��6$   $
��Left=�6L   $��
71,293,0,0q�6g   $	��Top=�6{   $��75��6�   
���   �
�������
�������6�   �� 5   
   -   5        $��
SendButton��5      $
��Send��6#   $
��Left=�6K   $��217,325,0,0q�6f   $	��Top=�6{   $��75��6�   
���   �
�������
�������6�   �� 5   
   -   5        $��DisconnectButton��5      $��
Disconnect��6)   $
��Left=�6]   $��323,293,0,0q�6x   $	��Top=�6�   $��75��6�   
���   ����	#FFD7D7D7
�������6�   �� 5   
   -   5        $��RefreshButton��5      $��Refresh��6&   $
��Left=�6N   $��	25,63,0,0q�6i   $	��Top=�6|   $��51��6�   
���   �
�������
�������6�   �� 5   
   -   5        $��Search for Folder��5      $
��Left=�6B   $��	25,26,0,0q�6]   $	��Top=�6p   $	��105��6�   
���   �
�������
�������6�   �� 5   
   -   5        $��FileListBox��5      $
��Left=�6%   $	��162��6@   $��504,113,0,0q�6M   $	��Top=�6b   $	��232��6z   �� 5   
   -   5        $��LabelChecking��5      $��Checking��6%   $
��Left=�68   $��
114,57,0,0q�6S   $	��Top=�6g   
������ 
�������6�   �� 5   
   $��Available IP Addresses��6   $
��Left=�61   $��	71,87,0,0q�6L   $	��Top=�6_   
�������6w   �� 5   
   $��Connected Devices��6   $
��Left=�6,   $��
288,87,0,0q�6G   $	��Top=�6[   
�������6s   �� 5   
   $��Files��6   $
��Left=�6    $��
504,87,0,0q�6;   $	��Top=�6O   
�������6g   �� 5   
   -   5        $��StatusLabel��5      $
��Left=�6#   $��336,325,0,0q�6>   $	��Top=�6S   
�������6k   �� 5   
   -	   5        $��AvailableIPAddressListBox��5      $
��Left=�63   $	��162��6N   $��
71,113,0,0q�6[   $	��Top=�6o   $	��154��6�   �� 5   
   -
   5        $��ConnectedIPAddressListBox��5      $
��Left=�63   $	��162��6N   $��288,113,0,0q�6[   $	��Top=�6p   $	��157��6�   �� 5   
   $
��Left=�6   $��82��6+   $��
700,10,0,0q�67   $	��Top=�6K   $��84��6c   $��../mascot.png��6n   5"      5#      