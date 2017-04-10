using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Windows;


namespace UltraDES
{
    internal static class FigureStream
    {

        public static void WriteSVGHeader(StreamWriter file, double width, double heigth)
        {
            width = Math.Round(width);
            heigth = Math.Round(heigth);

            file.WriteLine("<?xml version=\"1.0\" encoding=\"ISO-8859-1\" standalone=\"no\"?> ");
            file.WriteLine("<!DOCTYPE svg PUBLIC \"-//W3C//DTD SVG 20010904//EN\" \"http://www.w3.org/TR/2001/REC-SVG-20010904/DTD/svg10.dtd\"> ");
            file.WriteLine("<svg xmlns=\"http://www.w3.org/2000/svg\" xmlns:xlink=\"http://www.w3.org/1999/xlink\" xml:space=\"preserve\"" +
                    " width=\"" + width + "px\" height=\" " + heigth + "px\" viewBox=\"0 0 " + width + " " + heigth + "\" >  ");
        }

        public static void WriteLatexHeader(StreamWriter file, double width, double heigth)
        {
            width = Math.Round(width);
            heigth = Math.Round(heigth);

            file.WriteLine("\\centering");
            file.WriteLine("\\begin{tikzpicture}[scale=0.3, x=0.1 cm, y=0.1 cm]");
            file.WriteLine("\\tikzstyle{every node}+=[inner sep=0pt]");
        }

        public static void WriteSVGEnd(StreamWriter file)
        {
            file.WriteLine("</svg>");
        }

        public static void WriteLatexEnd(StreamWriter file)
        {
            file.WriteLine("\\end{tikzpicture}");
        }

        public static void drawSVGState(StreamWriter file, DrawingState state, int radius)
        {
            state.position = Drawing.RoundVector(state.position);

            file.WriteLine("\t <circle cx=\"" + state.position.X + "\" cy=\"" + state.position.Y + "\" r=\"" + radius + "\" stroke=\"black\" stroke-width=\"1\" fill=\"none\" /> \n");

            if (state.IsMarked)
            {
                file.WriteLine("\t <circle cx=\"" + state.position.X + "\" cy=\"" + state.position.Y + "\" r=\"" + (radius - 4) + "\" stroke=\"black\" stroke-width=\"1\" fill=\"none\" /> \n");
            }

            int fontSize = 17;
            Vector gap = new Vector(0, fontSize / 3);
            WriteSVGText(file, state.position, state.Alias, 0, gap, "none", fontSize);

        }

        public static void drawLatexState(StreamWriter file, DrawingState state, int radius, string fontSize)
        {
            state.position = Drawing.RoundVector(state.position);

            file.WriteLine("\\draw [black, line width= 0.8pt] " + state.position.ToString() + " circle (" + radius + ");");

            if (state.IsMarked)
            {
                file.WriteLine("\\draw [black, line width= 0.8pt] " + state.position.ToString() + " circle (" + (radius - 4) + ");");
            }

            WriteLatexText(file, state.position, state.Alias, fontSize, 0);
        }

        public static void drawSVGArrow(StreamWriter file, Vector arrowLocation, double arrowInclination, Vector inclinationReference)
        {

            int arrowLength = 15;
            double arrowAngle = Math.PI / 12;                //angulo de abertura da seta 15 graus
            Vector firstPosition = new Vector();
            Vector secondPosition = new Vector();
            double arrowInclinationDegrees = -(arrowInclination * 180) / Math.PI;

            //calculo das cordenadas dos vertices da seta
            firstPosition.X = (arrowLocation.X) + arrowLength;
            firstPosition.Y = (arrowLocation.Y) + arrowLength * Math.Tan(arrowAngle);

            secondPosition.X = firstPosition.X;
            secondPosition.Y = arrowLocation.Y - arrowLength * Math.Tan(arrowAngle);

            //escreve em arquivo SVG
            file.WriteLine("\t <polygon points=\"" + arrowLocation.X + "," + arrowLocation.Y + " " + firstPosition.X + "," +
                firstPosition.Y + " " + secondPosition.X + "," + secondPosition.Y +
                "\" style=\"fill:black;stroke:black;stroke-width:3\" transform =\"rotate(" + arrowInclinationDegrees +
                " " + (inclinationReference.X) + "," + inclinationReference.Y + ")\" /> ");

        }

        public static void drawLatexArrow(StreamWriter file, Vector arrowLocation, double arrowInclination, Vector inclinationReference)
        {

            int arrowLength = 15;
            double arrowAngle = Math.PI / 12;                //angulo de abertura da seta 15 graus
            Vector firstPosition = new Vector();
            Vector secondPosition = new Vector();
            double arrowInclinationDegrees = Math.Round((arrowInclination * 180) / Math.PI, Constants.NUMBER_OF_DIGITS_TO_ROUND);// (inclinacaoSeta * 180) / Math.PI;

            //calculo das cordenadas dos vertices da seta
            firstPosition.X = (arrowLocation.X) + arrowLength;
            firstPosition.Y = (arrowLocation.Y) + arrowLength * Math.Tan(arrowAngle);

            secondPosition.X = firstPosition.X;
            secondPosition.Y = arrowLocation.Y - arrowLength * Math.Tan(arrowAngle);

            //arredondamento dos valores para que o latex possa suportar arquivos grandes
            inclinationReference = Drawing.RoundVector(inclinationReference);
            arrowLocation = Drawing.RoundVector(arrowLocation);
            firstPosition = Drawing.RoundVector(firstPosition);
            secondPosition = Drawing.RoundVector(secondPosition);

            //escreve no arquivo .txt
            file.WriteLine("\\fill [black, rotate around={" + arrowInclinationDegrees + ":" + inclinationReference.ToString() + "} ] " +
                arrowLocation.ToString() + " -- " + firstPosition.ToString() + " -- " + secondPosition.ToString() + ";");
        }

        public static void drawSVGLine(StreamWriter file, Vector originPosition, Vector destinationPosition, double inclination, Vector inclinationReference)
        {
            double inclinationDegrees = (inclination * 180) / Math.PI;

            file.WriteLine("\t <line x1=\"" + originPosition.X + "\" y1=\"" + originPosition.Y + "\" x2=\"" + destinationPosition.X + "\" y2=\"" +
                destinationPosition.Y + "\" style=\"stroke:black;stroke-width:1\" transform =\"rotate(" + inclinationDegrees + " " + (inclinationReference.X) + "," +
                inclinationReference.Y + ")\" />");

            //escreve arquivo .svg (Por que você não arredondou acima?, ficou muito ruim?)
            //inclinationReference = Drawing.RoundVector(inclinationReference);
            //inclinationDegrees = Math.Round(inclinationDegrees, Constants.NUMBER_OF_DIGITS_TO_ROUND);
            //originPosition = Drawing.RoundVector(originPosition);
            //destinationPosition = Drawing.RoundVector(destinationPosition);
        }

        public static void drawLatexLine(StreamWriter file, Vector originPosition, Vector destinationPosition, double inclination, Vector inclinationReference)
        {
            double inclinationDegrees = Math.Round(-(inclination * 180) / Math.PI, Constants.NUMBER_OF_DIGITS_TO_ROUND);
            inclinationReference = Drawing.RoundVector(inclinationReference);
            originPosition = Drawing.RoundVector(originPosition);
            destinationPosition = Drawing.RoundVector(destinationPosition);

            //escreve arquivo latex

            file.WriteLine("\\draw [black, line width= 0.8pt, rotate around={" + inclinationDegrees + ":" + inclinationReference.ToString() + "}] " + originPosition.ToString() +
                " -- " + destinationPosition.ToString() + ";");

        }

        public static void TransicaoReta(StreamWriter file, DrawingState origem, DrawingState destino, 
            string nomeTransicao, string preenchimentoFonte, int tamanhoFonte)
        {
            Vector comprimento, pontoOrigemReta, pontoDestinoReta, localSeta;
            Vector localTexto = new Vector();
            Vector correcaoPosTexto = new Vector();
            double x, y, offsetanguloTexto = 0, aberturaTransicao = 0;
            double anguloEstado = 0, offsetAnguloSeta = 0, inclinacaoSeta = 0;

            comprimento = destino.position - origem.position;
            anguloEstado = Math.Atan2(-comprimento.Y, comprimento.X);

            if (anguloEstado < 0)
            {
                anguloEstado += 2 * Math.PI;
            }

            if (origem.IgualAnterior(destino))
            {
                aberturaTransicao = Constants.TRANSITION_OFFSET;
                offsetAnguloSeta = Math.Atan2(aberturaTransicao, comprimento.Length - Constants.STATE_RADIUS - Constants.DISTANCE);

                if (offsetAnguloSeta < 0)
                {
                    offsetAnguloSeta += 2 * Math.PI;
                }


            }

            x = origem.position.X + Constants.STATE_RADIUS + Constants.DISTANCE;
            y = origem.position.Y - aberturaTransicao;
            pontoOrigemReta = new Vector(x, y);

            x = origem.position.X + comprimento.Length - Constants.STATE_RADIUS - Constants.DISTANCE;
            y = pontoOrigemReta.Y;
            pontoDestinoReta = new Vector(x, y);

            x = origem.position.X + Math.Cos(anguloEstado + offsetAnguloSeta) * (comprimento.Length - Constants.STATE_RADIUS - Constants.DISTANCE);
            y = origem.position.Y - Math.Sin(anguloEstado + offsetAnguloSeta) * (comprimento.Length - Constants.STATE_RADIUS - Constants.DISTANCE);
            localSeta = new Vector(x, y);

            inclinacaoSeta = anguloEstado + Math.PI;

            drawSVGLine(file, pontoOrigemReta, pontoDestinoReta, -anguloEstado, origem.position);
            drawSVGArrow(file, localSeta, inclinacaoSeta, localSeta);


            offsetanguloTexto = Math.Atan2(Constants.TEXT_OFFSET + aberturaTransicao, comprimento.Length / 2);

            if (offsetanguloTexto < 0)
            {
                offsetanguloTexto += 2 * Math.PI;
            }

            //correcão do posicionamnto do texto quando angulo vara entre 90 e 270 graus

            if (anguloEstado >= Math.PI / 2 && anguloEstado <= 3 * Math.PI / 2)
            {
                correcaoPosTexto.X = -Math.Sin(anguloEstado) * Constants.TEXT_OFFSET;
                correcaoPosTexto.Y = -Math.Cos(anguloEstado) * Constants.TEXT_OFFSET;
            }


            x = origem.position.X + Math.Cos(offsetanguloTexto + anguloEstado) * (comprimento.Length / 2) + correcaoPosTexto.X;
            y = origem.position.Y - Math.Sin(offsetanguloTexto + anguloEstado) * (comprimento.Length / 2) + correcaoPosTexto.Y;


            Vector gap = new Vector();
            // gap=Geral.RetornaOffset(Constantes.OFFSETTEXTO, anguloEstado);

            WriteSVGText(file, localTexto, nomeTransicao, anguloEstado, gap, preenchimentoFonte, tamanhoFonte);


        }

        public static void TransicaoRetaLatex(StreamWriter fileLatex, DrawingState origem, DrawingState destino, string nomeTransicao, string tamanhoFonte)
        {
            Vector comprimento;
            Vector pontoOrigemReta = new Vector();
            Vector pontoDestinoReta = new Vector();
            Vector localSeta = new Vector();
            Vector localTexto = new Vector();
            double offsetanguloTexto = 0, aberturaTransicao = 0, anguloEstado = 0;
            double offsetAnguloSeta = 0, inclinacaoSeta = 0;


            comprimento = destino.position - origem.position;
            anguloEstado = Math.Atan2(-comprimento.Y, comprimento.X);

            if (anguloEstado < 0)
            {
                anguloEstado += 2 * Math.PI;
            }

            if (origem.IgualAnterior(destino))
            {
                aberturaTransicao = Constants.TRANSITION_OFFSET;
                offsetAnguloSeta = Math.Atan2(aberturaTransicao, comprimento.Length - Constants.STATE_RADIUS - Constants.DISTANCE);

                if (offsetAnguloSeta < 0)
                {
                    offsetAnguloSeta += 2 * Math.PI;
                }
            }

            pontoOrigemReta.X = origem.position.X + Constants.STATE_RADIUS + Constants.DISTANCE;
            pontoOrigemReta.Y = origem.position.Y - aberturaTransicao;

            pontoDestinoReta.X = origem.position.X + comprimento.Length - Constants.STATE_RADIUS - Constants.DISTANCE;
            pontoDestinoReta.Y = pontoOrigemReta.Y;


            localSeta.X = origem.position.X + Math.Cos(anguloEstado + offsetAnguloSeta) * (comprimento.Length - Constants.STATE_RADIUS - Constants.DISTANCE);
            localSeta.Y = origem.position.Y - Math.Sin(anguloEstado + offsetAnguloSeta) * (comprimento.Length - Constants.STATE_RADIUS - Constants.DISTANCE);

            inclinacaoSeta = anguloEstado + Math.PI;

            drawLatexLine(fileLatex, pontoOrigemReta, pontoDestinoReta, anguloEstado, origem.position);
            drawLatexArrow(fileLatex, localSeta, -inclinacaoSeta, localSeta);

            offsetanguloTexto = Math.Atan2(Constants.TEXT_OFFSET + aberturaTransicao, comprimento.Length / 2);

            if (offsetanguloTexto < 0)
            {
                offsetanguloTexto += 2 * Math.PI;
            }


            localTexto.X = origem.position.X + Math.Cos(-offsetanguloTexto + anguloEstado) * (comprimento.Length / 2);
            localTexto.Y = origem.position.Y - Math.Sin(-offsetanguloTexto + anguloEstado) * (comprimento.Length / 2);

            WriteLatexText(fileLatex, localTexto, nomeTransicao, tamanhoFonte, -anguloEstado);

        }

        public static void TransicaoCurva2(StreamWriter file, DrawingState origem, DrawingState destino, string nomeTransicao, string preenchimentoFonte, int tamanhoFonte)
        {

            Vector distanciaEstado = new Vector();
            distanciaEstado = destino.position - origem.position;
            double alturaCurva;
            double inclinacaoTransicao = Math.Atan2(-distanciaEstado.Y, distanciaEstado.X);
            double anguloInicioSeta = 0;
            double anguloFinalSeta = 0;
            double offset = 0;
            double correcaoAnguloSeta;
            double correcaoAnguloTexto = 0;
            double correcaoLocalTexto = 0;
            double anguloSeta;
            double pontoIncioArcoRef, comprimentoArco;
            Vector pontoIncioArco = new Vector();
            Vector pontoDestinoArco = new Vector();


            if (inclinacaoTransicao < 0)
            {
                inclinacaoTransicao += 2 * Math.PI;
            }

            double inclinacaoTransicaoGraus = -Math.Round(180 * inclinacaoTransicao / Math.PI);

            pontoIncioArcoRef = Math.Round(origem.position.X + Constants.STATE_RADIUS + Constants.DISTANCE);
            comprimentoArco = distanciaEstado.Length - 2 * (Constants.STATE_RADIUS + Constants.DISTANCE);
            alturaCurva = -Math.Round(comprimentoArco / 3);

            offset = -Constants.TRANSITION_OFFSET;
            anguloInicioSeta = Math.Atan2(-offset, (Constants.STATE_RADIUS + Constants.DISTANCE));
            anguloFinalSeta = Math.Atan2(-offset, (Constants.STATE_RADIUS + Constants.DISTANCE + comprimentoArco));

            correcaoAnguloSeta = Math.PI - Math.Atan2(-alturaCurva, comprimentoArco / 2);
            anguloSeta = inclinacaoTransicao + correcaoAnguloSeta;


            pontoIncioArco.X = origem.position.X + Math.Cos(inclinacaoTransicao + anguloInicioSeta) * (Constants.STATE_RADIUS + Constants.DISTANCE);
            pontoIncioArco.Y = origem.position.Y - Math.Sin(inclinacaoTransicao + anguloInicioSeta) * (Constants.STATE_RADIUS + Constants.DISTANCE);

            pontoDestinoArco.X = origem.position.X + Math.Cos(inclinacaoTransicao + anguloFinalSeta) * (Constants.STATE_RADIUS + Constants.DISTANCE + comprimentoArco);
            pontoDestinoArco.Y = origem.position.Y - Math.Sin(inclinacaoTransicao + anguloFinalSeta) * (Constants.STATE_RADIUS + Constants.DISTANCE + comprimentoArco);


            origem.position = Drawing.RoundVector(origem.position);
            destino.position = Drawing.RoundVector(destino.position);

            file.WriteLine("\t <path d=\"M " + pontoIncioArcoRef + " " + (origem.position.Y + offset) + " q " + Math.Round(comprimentoArco / 2) + " " + alturaCurva + " " + Math.Round(comprimentoArco) + " " + 0 +
                "\" stroke=\"black\" stroke-width=\"1\" fill=\"none\"  transform =\"rotate(" + inclinacaoTransicaoGraus + " " + (origem.position.X) + "," +
                origem.position.Y + ")\" />");


            drawSVGArrow(file, pontoDestinoArco, anguloSeta, pontoDestinoArco);

            correcaoLocalTexto = Constants.TEXT_OFFSET;
            if (inclinacaoTransicao > Math.PI / 2 && inclinacaoTransicao < 3 * Math.PI / 2)
            {
                correcaoLocalTexto = Constants.TEXT_OFFSET + 6;
            }

            Vector localTexto = new Vector();
            double distanciaOrigemSetaTexto = Math.Sqrt((-alturaCurva / 2 + correcaoLocalTexto) * (-alturaCurva / 2 + correcaoLocalTexto) + (comprimentoArco / 2) * (comprimentoArco / 2));
            correcaoAnguloTexto = Math.Atan2(-alturaCurva / 2 + correcaoLocalTexto, comprimentoArco / 2);

            localTexto.X = pontoIncioArco.X + Math.Cos(inclinacaoTransicao + correcaoAnguloTexto) * distanciaOrigemSetaTexto;
            localTexto.Y = pontoIncioArco.Y - Math.Sin(inclinacaoTransicao + correcaoAnguloTexto) * distanciaOrigemSetaTexto;
            localTexto = Drawing.RoundVector(localTexto);


            Vector gap = new Vector();

            WriteSVGText(file, localTexto, nomeTransicao, inclinacaoTransicao, gap, preenchimentoFonte, tamanhoFonte);


        }

        public static void TransicaoCurvaLatex(StreamWriter fileLatex, DrawingState origem, DrawingState destino, string nomeTransicao, string tamanhoFonte)
        {

            Vector inicioArco = new Vector();
            Vector destinoArco = new Vector();

            Vector destinoSeta = new Vector();

            Vector primeiroPontoArco = new Vector();
            Vector segundoPontoArco = new Vector();
            Vector distanciaEstados = new Vector();
            Vector localNomeTransicao = new Vector();
            Vector distanciaDestinoSeta = new Vector();           //distancia entre estado oriem e destino seta
            Vector localSeta = new Vector();
            double anguloInicioArco = Math.PI / 8;
            double anguloEstado;
            double anguloEstadoGraus;
            double anguloCorrecaoLocalseta;
            double inclinacaoSeta, anguloCorrecaoSeta;

            distanciaEstados = destino.position - origem.position;
            anguloEstado = Math.Atan2(distanciaEstados.Y, distanciaEstados.X);

            if (anguloEstado < 0)
            {
                anguloEstado += 2 * Math.PI;
            }

            anguloEstadoGraus = anguloEstado * 180 / Math.PI;

            //calculo do ponto de incio transiçao
            inicioArco.X = origem.position.X + Math.Cos(anguloInicioArco) * Constants.STATE_RADIUS;
            inicioArco.Y = origem.position.Y + Math.Sin(anguloInicioArco) * Constants.STATE_RADIUS;

            //calculo do ponto de destino transição
            destinoArco.X = origem.position.X + distanciaEstados.Length - Math.Cos(anguloInicioArco) * Constants.STATE_RADIUS;
            destinoArco.Y = inicioArco.Y;

            //calculo primeiro ponto do arco
            primeiroPontoArco.X = origem.position.X + distanciaEstados.Length / 3;
            primeiroPontoArco.Y = origem.position.Y + distanciaEstados.Length / 5;

            //calculo segundo ponto do arco
            segundoPontoArco.X = origem.position.X + 2 * distanciaEstados.Length / 3;
            segundoPontoArco.Y = origem.position.Y + distanciaEstados.Length / 5;


            //calculo posião seta

            distanciaDestinoSeta = destinoArco - origem.position;
            anguloCorrecaoLocalseta = Math.Atan2(distanciaDestinoSeta.Y, distanciaDestinoSeta.X);
            localSeta.X = origem.position.X + Math.Cos(anguloCorrecaoLocalseta + anguloEstado) * distanciaDestinoSeta.Length;
            localSeta.Y = origem.position.Y + Math.Sin(anguloCorrecaoLocalseta + anguloEstado) * distanciaDestinoSeta.Length;
            anguloCorrecaoSeta = Math.Atan2(primeiroPontoArco.Y - inicioArco.Y, primeiroPontoArco.X - inicioArco.X) * 0.75;
            inclinacaoSeta = anguloEstado + Math.PI - anguloCorrecaoSeta;




            //arredondamento
            inicioArco = Drawing.RoundVector(inicioArco);
            destinoArco = Drawing.RoundVector(destinoArco);
            primeiroPontoArco = Drawing.RoundVector(primeiroPontoArco);
            segundoPontoArco = Drawing.RoundVector(segundoPontoArco);
            anguloEstadoGraus = Math.Round(anguloEstadoGraus, Constants.NUMBER_OF_DIGITS_TO_ROUND);
            destinoSeta = Drawing.RoundVector(destinoSeta);



            fileLatex.WriteLine("\\draw[line width= 0.8pt, rotate around={" + anguloEstadoGraus + ":" + origem.position.ToString() + "}, line width=.5pt, smooth] " + inicioArco.ToString() +
                " .. controls " + primeiroPontoArco.ToString() + " and " + segundoPontoArco.ToString() + " .. " + destinoArco.ToString() + ";");

            //COLCOCAR SETA NA TRANSICAO

            drawLatexArrow(fileLatex, localSeta, inclinacaoSeta, localSeta);


            // COLOCAR A CODIGO PARA INSERIR NOME NAS TRANSIÇOES
            double correcaoAnguloTexto = Math.Atan2(distanciaEstados.Length / 4, distanciaEstados.Length / 2);
            
            localNomeTransicao.X = origem.position.X + Math.Cos(anguloEstado + correcaoAnguloTexto) * distanciaEstados.Length / 2;
            localNomeTransicao.Y = origem.position.Y + Math.Sin(anguloEstado + correcaoAnguloTexto) * distanciaEstados.Length / 2;
            localNomeTransicao = Drawing.RoundVector(localNomeTransicao);

            WriteLatexText(fileLatex, localNomeTransicao, nomeTransicao, tamanhoFonte, anguloEstado);



        }

        public static void AutoTransicao(StreamWriter file, Vector cordenadaEstado, double angulo, string nomeEventos, string preenchimentoFonte, int tamanhoFonte)  //angulo em radianos
        {
            double abertura = 6;
            double teta;
            Vector posicaoSeta = new Vector();
            Vector ponto = new Vector();

            ponto.X = Math.Round(cordenadaEstado.X + Math.Cos(angulo) * (Constants.STATE_RADIUS + 5));
            ponto.Y = Math.Round(cordenadaEstado.Y - Math.Sin(angulo) * (Constants.STATE_RADIUS + 5));

            Vector pontoAcima = Drawing.RoundVector(new Vector(ponto.X, ponto.Y - abertura));
            Vector pontoAcimaRef = Drawing.RoundVector(new Vector(pontoAcima.X - 10 * abertura, pontoAcima.Y - abertura));
            Vector pontoAbaixo = Drawing.RoundVector(new Vector(ponto.X, ponto.Y + abertura));
            Vector pontoAbaixoRef = Drawing.RoundVector(new Vector(pontoAbaixo.X - 10 * abertura, pontoAbaixo.Y + abertura));

            teta = -(angulo - Math.PI);
            double tetaGraus = Math.Round((teta * 180) / Math.PI, Constants.NUMBER_OF_DIGITS_TO_ROUND);


            file.WriteLine("\t <path d=\"M" + pontoAcima.X + " " + pontoAcima.Y + " " + "C" + " " + pontoAcimaRef.X + " " + pontoAcimaRef.Y + ", " +
                pontoAbaixoRef.X + " " + pontoAbaixoRef.Y + ", " + pontoAbaixo.X + " " + pontoAbaixo.Y +
                "\" stroke=\"black\" fill=\"transparent\"  transform=\"rotate(" + tetaGraus + " " + ponto.X + ", " + ponto.Y + ")\" />");

            posicaoSeta.X = ponto.X + Math.Cos(3 * Math.PI / 2 - teta) * abertura;
            posicaoSeta.Y = ponto.Y - Math.Sin(3 * Math.PI / 2 - teta) * abertura;
            posicaoSeta = Drawing.RoundVector(posicaoSeta);

            drawSVGArrow(file, posicaoSeta, -teta + Math.PI, posicaoSeta);

            // localização do local do texto
            Vector distanciaTexto = new Vector();
            Vector pontoNomeTransicao = new Vector();
            Vector gap = new Vector();

            angulo = angulo % (2 * Math.PI);
            if ((angulo > Math.PI / 2) && (angulo < 3 * Math.PI / 2))
            {
                distanciaTexto.X = Math.Sin(angulo) * (Constants.TEXT_OFFSET + 7);
                distanciaTexto.Y = Math.Cos(angulo) * (Constants.TEXT_OFFSET + 7);
            }

            if ((angulo >= 0 && angulo <= Math.PI / 2) || (angulo >= 3 * Math.PI / 2 && angulo < 2 * Math.PI))
            {
                distanciaTexto.X = -Math.Sin(angulo) * (Constants.TEXT_OFFSET + 7);
                distanciaTexto.Y = -Math.Cos(angulo) * (Constants.TEXT_OFFSET + 7);
            }


            pontoNomeTransicao.X = cordenadaEstado.X + 2.5 * Math.Cos(angulo) * Constants.STATE_RADIUS + distanciaTexto.X;
            pontoNomeTransicao.Y = cordenadaEstado.Y - 2.5 * Math.Sin(angulo) * Constants.STATE_RADIUS + distanciaTexto.Y;
            pontoNomeTransicao = Drawing.RoundVector(pontoNomeTransicao);

            WriteSVGText(file, pontoNomeTransicao, nomeEventos, angulo, gap, preenchimentoFonte, tamanhoFonte);


        }

        public static void AutoTransicaoLatex(StreamWriter fileLatex, Vector cordenadaEstado, double angulo, string nomeEventos)  //angulo em radianos
        {
            angulo *= -1;
            double anguloInicioArco = Math.PI / 15;
            double correcaoAnguloSeta, correcaoAnguloTexto;
            Vector localNomeTransicao = new Vector();
            Vector parametroReferencia = new Vector(50, 10);
            Vector inicioAtutoTransicao = new Vector();
            Vector destinoAtutoTransicao = new Vector();
            Vector localSeta = new Vector();
            Vector primeiraReferenciaAtutoTransicao = new Vector();
            Vector segundaReferenciaAtutoTransicao = new Vector();
            double anguloGraus = Math.Round(angulo * 180 / Math.PI, Constants.NUMBER_OF_DIGITS_TO_ROUND);

            // Calculo ponto incio Arco
            inicioAtutoTransicao.X = cordenadaEstado.X + Math.Cos(anguloInicioArco) * Constants.STATE_RADIUS;
            inicioAtutoTransicao.Y = cordenadaEstado.Y - Math.Sin(anguloInicioArco) * Constants.STATE_RADIUS;

            // Calculo ponto destino Arco
            destinoAtutoTransicao.X = inicioAtutoTransicao.X;
            destinoAtutoTransicao.Y = cordenadaEstado.Y + Math.Sin(anguloInicioArco) * Constants.STATE_RADIUS;

            // Calculo ponto primeira referencia Arco
            primeiraReferenciaAtutoTransicao.X = inicioAtutoTransicao.X + parametroReferencia.X;
            primeiraReferenciaAtutoTransicao.Y = inicioAtutoTransicao.Y - parametroReferencia.Y;

            // Calculo ponto primeira referencia Arco
            segundaReferenciaAtutoTransicao.X = inicioAtutoTransicao.X + parametroReferencia.X;
            segundaReferenciaAtutoTransicao.Y = destinoAtutoTransicao.Y + parametroReferencia.Y;

            //arredondamentos
            inicioAtutoTransicao = Drawing.RoundVector(inicioAtutoTransicao);
            destinoAtutoTransicao = Drawing.RoundVector(destinoAtutoTransicao);
            primeiraReferenciaAtutoTransicao = Drawing.RoundVector(primeiraReferenciaAtutoTransicao);
            segundaReferenciaAtutoTransicao = Drawing.RoundVector(segundaReferenciaAtutoTransicao);
            cordenadaEstado = Drawing.RoundVector(cordenadaEstado);

            // gera código para desenho em latex

            fileLatex.WriteLine("\\draw[ line width= 0.8pt, rotate around={" + anguloGraus + ":" + cordenadaEstado.ToString() + "}, line width=.5pt, smooth] " + inicioAtutoTransicao.ToString() +
                " .. controls " + primeiraReferenciaAtutoTransicao.ToString() + " and " + segundaReferenciaAtutoTransicao.ToString() + " .. " + destinoAtutoTransicao.ToString() + ";");

            //correçao angulo seta
            correcaoAnguloSeta = Math.Atan2(parametroReferencia.Y, parametroReferencia.X + Constants.STATE_RADIUS);

            //caculo ponto local seta
            localSeta.X = cordenadaEstado.X + Math.Cos(angulo + anguloInicioArco) * Constants.STATE_RADIUS;
            localSeta.Y = cordenadaEstado.Y + Math.Sin(angulo + anguloInicioArco) * Constants.STATE_RADIUS;

            drawLatexArrow(fileLatex, localSeta, angulo + correcaoAnguloSeta, localSeta);


            //calculo local nome transição
            correcaoAnguloTexto = Math.Atan2(parametroReferencia.Y + 10, parametroReferencia.X + Constants.STATE_RADIUS - 10);
            localNomeTransicao.X = cordenadaEstado.X + Math.Cos(angulo + correcaoAnguloTexto) * (Constants.STATE_RADIUS + parametroReferencia.X - 10);
            localNomeTransicao.Y = cordenadaEstado.Y + Math.Sin(angulo + correcaoAnguloTexto) * (Constants.STATE_RADIUS + parametroReferencia.X - 10);

            WriteLatexText(fileLatex, localNomeTransicao, nomeEventos, "footnotesize", angulo);




        }

        public static void WriteSVGText(StreamWriter file, Vector ponto, string texto, double tetaRadianos, Vector gap, string preenchimentoFonte, int tamanhofonte)
        {
            //Vector pontoRound = new Vector(Math.Round(ponto.X), Math.Round(ponto.Y));
            double tetaGrau = (180 * tetaRadianos) / Math.PI;
            //tetaGrau = Math.Round(tetaGrau);

            if (tetaGrau < 0)
            {
                tetaGrau += 360;
            }

            if (tetaGrau > 90 && tetaGrau <= 180)
            {
                tetaGrau += 180;
            }
            else if (tetaGrau > 180 && tetaGrau < 270)
            {
                tetaGrau = (tetaGrau + 180) % 360;
            }
            gap = Drawing.RoundVector(gap);


            file.WriteLine("<text x=\"" + ponto.X + "\" y=\"" + ponto.Y +
                "\" dx=\"" + gap.X + "\" dy=\"" + gap.Y + "\" font-size=\"" + tamanhofonte + "\" fill=\"black\" stroke=\"" +
                preenchimentoFonte + "\" text-anchor=\"middle\" transform=\"rotate(-" + tetaGrau + " " + ponto.X + "," + ponto.Y + ")\">" + texto + "</text>");

        }

        //escreve texo no arquivo latex
        public static void WriteLatexText(StreamWriter fileLatex, Vector ponto, string texto, string tamanhoFonte, double tetaRadianos)
        {
            if (tetaRadianos<0)
            {
                tetaRadianos += 2 * Math.PI;
            }
            tetaRadianos %= (2 * Math.PI);
            //Vector pontoRound = new Vector(Math.Round(ponto.X), Math.Round(ponto.Y));
            double tetaGrau = (180 * tetaRadianos) / Math.PI;


            if (tetaGrau < 0)
            {
                tetaGrau += 360;
            }

            if (tetaGrau > 90 && tetaGrau <= 180)
            {
                tetaGrau += 180;
            }
            else if (tetaGrau > 180 && tetaGrau < 270)
            {
                tetaGrau = (tetaGrau + 180) % 360;
            }

            ponto = Drawing.RoundVector(ponto);
            tetaGrau = Math.Round(tetaGrau, Constants.NUMBER_OF_DIGITS_TO_ROUND);

            fileLatex.WriteLine("\\draw " + ponto.ToString() + " node [rotate= " + tetaGrau + "] {\\" + tamanhoFonte + " $" + texto + "$};");


        }

        public static void TransicaoDrawing(StreamWriter file, Dictionary<string, DrawingState> listaEstados,int transicao)
        {
            Vector comprimento = new Vector();
            Vector origemSeta = new Vector();
            Vector destinoSeta = new Vector();


            foreach (var item in listaEstados)
            {
                foreach (var elemento in item.Value.estadosDestino)
                {
                    string nomeEventos = elemento.Value.Item1;

                    if (item.Value.Alias != elemento.Key.Alias)
                    {
                        if (transicao.Equals(0))
                        {
                            TransicaoReta(file, item.Value, elemento.Key, nomeEventos, "none", 18);
                        }
                        else if (transicao.Equals(1))
                        {
                            TransicaoCurva2(file, item.Value, elemento.Key, nomeEventos, "none", 18);
                        }
                        else
                        {
                            if (elemento.Key.IgualDestino(item.Value))
                            {
                                TransicaoCurva2(file, item.Value, elemento.Key, nomeEventos, "none", 18);
                            }
                            else
                            {
                                TransicaoReta(file, item.Value, elemento.Key, nomeEventos, "none", 18);
                            }
                        }
                        
                        
                    }
                    else
                    {
                        //calcula posiçao para inserir a auto transição

                        List<double> angulos = new List<double>();

                        double abertura = Math.PI / 4;
                        double anguloEstado;

                        foreach (var objeto in item.Value.estadosDestino)
                        {
                            comprimento = objeto.Key.position - item.Value.position;
                            anguloEstado = Math.Atan2(-comprimento.Y, comprimento.X);

                            if (anguloEstado < 0)
                            {
                                anguloEstado += 2 * Math.PI;
                            }

                            if (!angulos.Contains(anguloEstado) && !anguloEstado.Equals(0))
                            {
                                angulos.Add(anguloEstado);
                            }
                        }
                        foreach (var objeto in item.Value.estadosAnterior)
                        {
                            comprimento = objeto.Key.position - item.Value.position;
                            anguloEstado = Math.Atan2(-comprimento.Y, comprimento.X);

                            if (anguloEstado < 0)
                            {
                                anguloEstado += 2 * Math.PI;
                            }

                            if (!angulos.Contains(anguloEstado) && !anguloEstado.Equals(0))
                            {
                                angulos.Add(anguloEstado);
                            }
                        }

                        angulos.Sort();                     //organiza os Vector de angulos em ordem crescente

                        if (item.Value.initialState)
                        {
                            abertura = 3 * Math.PI / 4;
                        }
                        else
                        {
                            if (angulos.Count() > 1)
                            {
                                int iterador = angulos.Count() - 1;
                                if (angulos[iterador] - angulos[iterador - 1] > 2 * Math.PI - angulos[iterador] + angulos[0])
                                {
                                    abertura = ((angulos[iterador] - angulos[iterador - 1]) / 2 + angulos[iterador - 1]) % (2 * Math.PI);
                                }
                                else
                                {
                                    abertura = ((2 * Math.PI - angulos[iterador] + angulos[0]) / 2 + angulos[iterador]) % (2 * Math.PI);
                                }

                            }
                            else
                            {
                                abertura = angulos[0] + Math.PI;
                            }
                        }

                        AutoTransicao(file, item.Value.position, abertura, nomeEventos, "none", 18);

                    }



                    if (item.Value.initialState)
                    {
                        origemSeta.X = item.Value.position.X - (Constants.STATE_RADIUS + Constants.DISTANCE + 50);
                        origemSeta.Y = item.Value.position.Y;

                        destinoSeta.X = item.Value.position.X - (Constants.STATE_RADIUS + Constants.DISTANCE);
                        destinoSeta.Y = item.Value.position.Y;

                        Vector referencia = new Vector();

                        drawSVGLine(file, origemSeta, destinoSeta, 0, referencia);
                        drawSVGArrow(file, destinoSeta, Math.PI, destinoSeta);



                    }

                }

            }
        }

        public static void TransicaoDrawinglLatex(StreamWriter fileLatex, Dictionary<string, DrawingState> listaEstados, int transicao, string tamanhofonte)
        {
            Vector comprimento = new Vector();
            Vector origemSeta = new Vector();
            Vector destinoSeta = new Vector();


            foreach (var item in listaEstados)
            {
                foreach (var elemento in item.Value.estadosDestino)
                {

                    //string nomeEventos = Geral.GeraStringTransicoes(elemento.estado, item.Value.estadosDestino);
                    string nomeEventos = elemento.Value.Item1;


                    if (item.Value.Alias != elemento.Key.Alias)
                    {
                        if (transicao.Equals(0))
                        {
                            TransicaoRetaLatex(fileLatex, item.Value, elemento.Key, nomeEventos, tamanhofonte);
                        }
                        else if(transicao.Equals(1))
                        {
                            TransicaoCurvaLatex(fileLatex, item.Value, elemento.Key, nomeEventos, tamanhofonte);
                        }
                        else
                        {
                            if (elemento.Key.IgualDestino(item.Value))
                            {
                                TransicaoCurvaLatex(fileLatex, item.Value, elemento.Key, nomeEventos, tamanhofonte);
                            }
                            else
                            {
                                TransicaoRetaLatex(fileLatex, item.Value, elemento.Key, nomeEventos, tamanhofonte);
                            }
                        }

                    }
                    else
                    {
                        //calcula posiçao para inserir a auto transição

                        List<double> angulos = new List<double>();

                        double abertura = Math.PI / 4;
                        double anguloEstado;

                        foreach (var objeto in item.Value.estadosDestino)
                        {
                            comprimento = objeto.Key.position - item.Value.position;
                            anguloEstado = Math.Atan2(-comprimento.Y, comprimento.X);

                            if (anguloEstado < 0)
                            {
                                anguloEstado += 2 * Math.PI;
                            }

                            if (!angulos.Contains(anguloEstado) && !anguloEstado.Equals(0))
                            {
                                angulos.Add(anguloEstado);
                            }
                        }
                        foreach (var objeto in item.Value.estadosAnterior)
                        {
                            comprimento = objeto.Key.position - item.Value.position;
                            anguloEstado = Math.Atan2(-comprimento.Y, comprimento.X);

                            if (anguloEstado < 0)
                            {
                                anguloEstado += 2 * Math.PI;
                            }

                            if (!angulos.Contains(anguloEstado) && !anguloEstado.Equals(0))
                            {
                                angulos.Add(anguloEstado);
                            }
                        }

                        angulos.Sort();                     //organiza os Vector de angulos em ordem crescente

                        if (item.Value.initialState)
                        {
                            abertura = 3 * Math.PI / 4;
                        }
                        else
                        {
                            if (angulos.Count() > 1)
                            {
                                int iterador = angulos.Count() - 1;
                                if (angulos[iterador] - angulos[iterador - 1] > 2 * Math.PI - angulos[iterador] + angulos[0])
                                {
                                    abertura = ((angulos[iterador] - angulos[iterador - 1]) / 2 + angulos[iterador - 1]) % (2 * Math.PI);
                                }
                                else
                                {
                                    abertura = ((2 * Math.PI - angulos[iterador] + angulos[0]) / 2 + angulos[iterador]) % (2 * Math.PI);
                                }

                            }
                            else
                            {
                                abertura = angulos[0] + Math.PI;
                            }
                        }

                        AutoTransicaoLatex(fileLatex, item.Value.position, abertura, nomeEventos);

                    }

                    if (item.Value.initialState)
                    {
                        origemSeta.X = item.Value.position.X - (Constants.STATE_RADIUS + Constants.DISTANCE + 50);
                        origemSeta.Y = item.Value.position.Y;

                        destinoSeta.X = item.Value.position.X - (Constants.STATE_RADIUS + Constants.DISTANCE);
                        destinoSeta.Y = item.Value.position.Y;

                        Vector referencia = new Vector();

                        drawLatexLine(fileLatex, origemSeta, destinoSeta, 0, referencia);
                        drawLatexArrow(fileLatex, destinoSeta, Math.PI, destinoSeta);
                    }
                }
            }
        }
    }
}
