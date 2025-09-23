<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform" 
                 xmlns:xhtml="http://www.w3.org/1999/xhtml" 
                 version="1.0">
	<xsl:template match="/">
		<HTML>
			<BODY style = "margin-top: 0; margin-left: 4; margin-bottom: 0; margin-right: 0;">
				<xsl:apply-templates/>
			</BODY>
		</HTML>
	</xsl:template>

	
	
	<xsl:template match='tgroup|entrytbl' priority='10'>
		<table>
			
			
			<xsl:if test='colspec[contains(@colwidth,"*") or not(@colwidth)]'>
				<xsl:attribute name='width'>100%</xsl:attribute>
			</xsl:if>
			
			
			<xsl:attribute name='style'>
				border-collapse: collapse;
				border-width: 1px;
				border-color: black;
				
				<xsl:variable name='frame' select='ancestor::table/@frame'/>
				<xsl:choose>
					<xsl:when test='$frame="sides"'>
						border-left-style: solid;
						border-right-style: solid;
						border-top-style: hidden;
						border-bottom-style: hidden;
					</xsl:when>
					<xsl:when test='$frame="topbot"'>
						border-left-style: none;
						border-right-style: none;
						border-top-style: hidden;
						border-bottom-style: hidden;
					</xsl:when>
					<xsl:when test='$frame="top"'>
						border-left-style: hidden;
						border-right-style: hidden;
						border-top-style: solid;
						border-bottom-style: hidden;
					</xsl:when>
					<xsl:when test='$frame="bottom"'>
						border-left-style: hidden;
						border-right-style: hidden;
						border-top-style: hidden;
						border-bottom-style: solid;
					</xsl:when>
					<xsl:when test='$frame="none"'>
						border-left-style: hidden;
						border-right-style: hidden;
						border-top-style: hidden;
						border-bottom-style: hidden;
					</xsl:when>
					<xsl:otherwise>
						border-left-style: solid;
						border-right-style: solid;
						border-top-style: solid;
						border-bottom-style: solid;
					</xsl:otherwise>
				</xsl:choose>
			</xsl:attribute>
		
			
			<xsl:for-each select='colspec'>
				<col>
				
					
					<xsl:variable name='colwidth' select='@colwidth'/>
					<xsl:choose>
                        
                        
						<xsl:when test='contains($colwidth,"+") or not($colwidth) or $colwidth="*"'>
							<xsl:attribute name='width'>1*</xsl:attribute>
						</xsl:when>
						<xsl:when test='contains($colwidth,"*")'>
							<xsl:attribute name='width'><xsl:value-of select='$colwidth'/></xsl:attribute>
						</xsl:when>
						<xsl:otherwise>
							<xsl:attribute name="style">width: 
								<xsl:choose>
									<xsl:when test='contains($colwidth,"pi")'>
										
										<xsl:value-of select='translate($colwidth,"i","c")'/>
									</xsl:when>
									<xsl:otherwise>
										<xsl:value-of select='$colwidth'/>
									</xsl:otherwise>
								</xsl:choose>
							</xsl:attribute>
						</xsl:otherwise>
					</xsl:choose>
					
				</col>
			</xsl:for-each>
			
			
			<xsl:for-each select='thead|tfoot|tbody'>
				<xsl:copy>
					<xsl:for-each select='row'>
						<tr>
							
							<xsl:for-each select='entry|entrytbl'>
								<xsl:call-template name='transformCALSCell'/>
							</xsl:for-each>
						</tr>
					</xsl:for-each>
				</xsl:copy>
			</xsl:for-each>
		</table>
	</xsl:template>
	
	<xsl:template name='transformCALSCell'>
		<td>
			
			<xsl:variable name='spanname' select='@spanname'/>
			<xsl:variable name='namest' select='@namest'/>
			<xsl:variable name='nameend' select='@nameend'/>
			
			<xsl:choose>
				<xsl:when test='$spanname'>
					<xsl:variable name='spanspec' select='ancestor::*[spanspec][1]/spanspec[@spanname=$spanname]'/>
					<xsl:variable name='namest_2' select='$spanspec/@namest'/>
					<xsl:variable name='nameend_2' select='$spanspec/@nameend'/>
					
					<xsl:variable name='startColumn' select='count(ancestor::*[colspec][1]/colspec[@colname=$namest_2]/preceding-sibling::*)'/>
					<xsl:variable name='endColumn' select='count(ancestor::*[colspec][1]/colspec[@colname=$nameend_2]/preceding-sibling::*)'/>
					
					<xsl:attribute name='colspan'><xsl:value-of select='$endColumn - $startColumn + 1'/></xsl:attribute>
					
				</xsl:when>
				
				<xsl:when test='$namest and $nameend'>
					<xsl:variable name='startColumn' select='count(ancestor::*[colspec][1]/colspec[@colname=$namest]/preceding-sibling::*)'/>
					<xsl:variable name='endColumn' select='count(ancestor::*[colspec][1]/colspec[@colname=$nameend]/preceding-sibling::*)'/>
					
					<xsl:attribute name='colspan'><xsl:value-of select='$endColumn - $startColumn + 1'/></xsl:attribute>
				</xsl:when>
			</xsl:choose>
			
			
			<xsl:variable name='morerows' select='@morerows'/>
			<xsl:if test='$morerows'>
				<xsl:attribute name='rowspan'><xsl:value-of select='$morerows+1'/></xsl:attribute>
			</xsl:if>
			
			
			<xsl:call-template name='findInheritedAttribute'>
				<xsl:with-param name='attributeName'>align</xsl:with-param>
			</xsl:call-template>
			<xsl:call-template name='findInheritedAttribute'>
				<xsl:with-param name='attributeName'>char</xsl:with-param>
			</xsl:call-template>
			<xsl:call-template name='findInheritedAttribute'>
				<xsl:with-param name='attributeName'>charoff</xsl:with-param>
			</xsl:call-template>
			
			
			<xsl:variable name='valign' select='ancestor-or-self::*[@valign][1]/@valign'/>
			<xsl:attribute name='valign'>
				<xsl:choose>
					<xsl:when test='$valign'>
						<xsl:value-of select='$valign'/>
					</xsl:when>
					<xsl:otherwise>
						
						<xsl:choose>
							<xsl:when test='parent::*/parent::thead'>
								bottom
							</xsl:when>
							<xsl:otherwise>
								top
							</xsl:otherwise>
						</xsl:choose>
					</xsl:otherwise>
				</xsl:choose>
			</xsl:attribute>
			
			
			<xsl:attribute name='style'>
				border-width: 1px;
				border-color: black;
				
				<xsl:call-template name='findInheritedAttribute'>
					<xsl:with-param name='attributeName'>colsep</xsl:with-param>
				</xsl:call-template>
				
				<xsl:call-template name='findInheritedAttribute'>
					<xsl:with-param name='attributeName'>rowsep</xsl:with-param>
				</xsl:call-template>
			</xsl:attribute>
			
			
			<xsl:apply-templates select='.'/>
		</td>
	</xsl:template>
	
	
	
	<xsl:template name='findInheritedAttribute'>
		<xsl:param name='attributeName'/>
		
		<xsl:variable name='colname' select='@colname'/>
		<xsl:variable name='colname_colspec' select='ancestor::*[colspec][1]/colspec[@colname=$colname]'/>
		
		<xsl:variable name='namest' select='@namest'/>
		<xsl:variable name='namest_colspec' select='ancestor::*[colspec][1]/colspec[@colname=$namest]'/>
		
		<xsl:variable name='spanname' select='@spanname'/>
		<xsl:variable name='spanspec' select='ancestor::*[spanspec][1]/spanspec[@spanname=$spanname]'/>
		<xsl:variable name='span_namest' select='$spanspec/@namest'/>
		<xsl:variable name='span_colspec' select='ancestor::*[colspec][1]/colspec[@colname=$span_namest]'/>
		
		
		<xsl:choose>
			
			<xsl:when test='@*[local-name()=$attributeName]'>
				<xsl:call-template name='transformInheritedAttribute'>
					<xsl:with-param name='attributeName' select='$attributeName'/>
					<xsl:with-param name='attributeValue' select='@*[local-name()=$attributeName]'/>
				</xsl:call-template>
			</xsl:when>
			
			
			<xsl:when test='parent::*/@*[local-name()=$attributeName]'>
				<xsl:call-template name='transformInheritedAttribute'>
					<xsl:with-param name='attributeName' select='$attributeName'/>
					<xsl:with-param name='attributeValue' select='parent::*/@*[local-name()=$attributeName]'/>
				</xsl:call-template>
			</xsl:when>
			
			
			<xsl:when test='$spanspec/@*[local-name()=$attributeName]'>
				<xsl:call-template name='transformInheritedAttribute'>
					<xsl:with-param name='attributeName' select='$attributeName'/>
					<xsl:with-param name='attributeValue' select='$spanspec/@*[local-name()=$attributeName]'/>
				</xsl:call-template>
			</xsl:when>
			
			
			
			<xsl:when test='$colname_colspec/@*[local-name()=$attributeName]'>
				<xsl:call-template name='transformInheritedAttribute'>
					<xsl:with-param name='attributeName' select='$attributeName'/>
					<xsl:with-param name='attributeValue' select='$colname_colspec/@*[local-name()=$attributeName]'/>
				</xsl:call-template>
			</xsl:when>
			<xsl:when test='$span_colspec/@*[local-name()=$attributeName]'>
				<xsl:call-template name='transformInheritedAttribute'>
					<xsl:with-param name='attributeName' select='$attributeName'/>
					<xsl:with-param name='attributeValue' select='$span_colspec/@*[local-name()=$attributeName]'/>
				</xsl:call-template>
			</xsl:when>	
			<xsl:when test='$namest_colspec/@*[local-name()=$attributeName]'>
				<xsl:call-template name='transformInheritedAttribute'>
					<xsl:with-param name='attributeName' select='$attributeName'/>
					<xsl:with-param name='attributeValue' select='$namest_colspec/@*[local-name()=$attributeName]'/>
				</xsl:call-template>
			</xsl:when>
			
			
			<xsl:when test='parent::*/parent::*/parent::*/@*[local-name()=$attributeName]'>
				<xsl:call-template name='transformInheritedAttribute'>
					<xsl:with-param name='attributeName' select='$attributeName'/>
					<xsl:with-param name='attributeValue' select='parent::*/parent::*/parent::*/@*[local-name()=$attributeName]'/>
				</xsl:call-template>
			</xsl:when>
							
			
			<xsl:when test='ancestor::table[1]/@*[local-name()=$attributeName]'>
				<xsl:call-template name='transformInheritedAttribute'>
					<xsl:with-param name='attributeName' select='$attributeName'/>
					<xsl:with-param name='attributeValue' select='ancestor::table[1]/@*[local-name()=$attributeName]'/>
				</xsl:call-template>
			</xsl:when>					
			
			
			<xsl:otherwise>
				<xsl:call-template name='transformInheritedAttribute'>
					<xsl:with-param name='attributeName' select='$attributeName'/>
					<xsl:with-param name='attributeValue' select='""'/>
				</xsl:call-template>
			</xsl:otherwise>
			
		</xsl:choose>
	</xsl:template>
	
	
	<xsl:template name='transformInheritedAttribute'>
		<xsl:param name='attributeName'/>
		<xsl:param name='attributeValue'/>
		
		<xsl:choose>
			
			
			<xsl:when test='$attributeName = "colsep"'>
				<xsl:choose>
					<xsl:when test='$attributeValue = "0"'>
						border-right-style: none;
					</xsl:when>
					<xsl:otherwise>
						border-right-style: solid;
					</xsl:otherwise>
				</xsl:choose>
			</xsl:when>
			
			
			
			<xsl:when test='$attributeName = "rowsep"'>
				<xsl:choose>
					<xsl:when test='$attributeValue = "0"'>
						border-bottom-style: none;
					</xsl:when>
					<xsl:otherwise>
						border-bottom-style: solid;
					</xsl:otherwise>
				</xsl:choose>
			</xsl:when>
			
			
			
			<xsl:when test='$attributeName = "align"'>
				<xsl:attribute name='align'>
					<xsl:choose>
						<xsl:when test='$attributeValue'>
							<xsl:value-of select="$attributeValue"/>
						</xsl:when>
						<xsl:otherwise>
							left
						</xsl:otherwise>
					</xsl:choose>
				</xsl:attribute>
			</xsl:when>
			
			
			
			<xsl:when test='$attributeName = "char"'>
				<xsl:if test='$attributeValue'>
					<xsl:attribute name='char'>
						<xsl:value-of select="$attributeValue"/>
					</xsl:attribute>
				</xsl:if>
			</xsl:when>
			
			
			
			<xsl:when test='$attributeName = "charoff"'>
				<xsl:attribute name='charoff'>
					<xsl:choose>
						<xsl:when test='$attributeValue'>
							
							<xsl:value-of select="$attributeValue"/>%
						</xsl:when>
						<xsl:otherwise>
							50%
						</xsl:otherwise>
					</xsl:choose>
				</xsl:attribute>
			</xsl:when>
		
		</xsl:choose>
	</xsl:template>
</xsl:stylesheet>
