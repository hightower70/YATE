@zmac zexall_tvc.asm
@copy /b /y .\zout\zexall_tvc.cim .\zout\zexall_tvc.bin
@del zexall_tvc.cas
@tvctape .\zout\zexall_tvc.bin zexall_tvc.cas

@zmac zexdoc_tvc.asm
@copy /b /y .\zout\zexdoc_tvc.cim .\zout\zexdoc_tvc.bin
@del zexdoc_tvc.cas
@tvctape .\zout\zexdoc_tvc.bin zexdoc_tvc.cas
