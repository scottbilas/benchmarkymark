foreach ($iters in 10,100,1000) {
    write-host -nonew "#$iters $((get-date).tostring("HH:mm:ss")) "
    $csv = "build\timings-$iters.csv"

    del -ea:silent $csv

    foreach ($i in 0..9) {
        foreach ($app in (fd app.exe$ build)) {
            & $app $iters | out-file -encoding ascii -append -file build\timings-$iters.csv
            write-host -nonew '.'
        }
        write-host -nonew '+'
    }

    write-host
}
